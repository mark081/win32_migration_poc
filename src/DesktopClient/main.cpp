#define UNICODE
#define _UNICODE
#include <windows.h>
#include <winhttp.h>
#include <string>
#include <sstream>
#include <iomanip>
#include "../NativeRules/NativeRules.h"
#pragma comment(lib, "winhttp.lib")

enum
{
    ID_REFRESH = 101,
    ID_LOAD_MEMBER,
    ID_CHECKOUT,
    ID_RETURN,
    ID_TOOL,
    ID_MEMBER,
    ID_DUE,
    ID_LOAN,
    ID_OUTPUT
};
static HWND outputBox, toolBox, memberBox, dueBox, loanBox;
static const wchar_t *API_KEY = L"demo-local-key";
static std::string Utf8(const std::wstring &s)
{
    if (s.empty())
        return {};
    int n = WideCharToMultiByte(CP_UTF8, 0, s.c_str(), -1, nullptr, 0, nullptr, nullptr);
    std::string x(n, 0);
    WideCharToMultiByte(CP_UTF8, 0, s.c_str(), -1, &x[0], n, nullptr, nullptr);
    x.pop_back();
    return x;
}
static std::wstring Wide(const std::string &s)
{
    if (s.empty())
        return {};
    int n = MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.size(), nullptr, 0);
    std::wstring x(n, 0);
    MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.size(), &x[0], n);
    return x;
}
static std::wstring Http(const wchar_t *verb, const std::wstring &path,
                         const std::string &body = "", const std::wstring &key = L"")
{
    HINTERNET session = WinHttpOpen(L"ToolLendingLegacyClient/1.0", WINHTTP_ACCESS_TYPE_NO_PROXY,
                                    nullptr, nullptr, 0);
    if (!session)
        return L"ERROR: WinHttpOpen";
    HINTERNET connect = WinHttpConnect(session, L"localhost", 8088, 0);
    HINTERNET request = WinHttpOpenRequest(connect, verb, path.c_str(), nullptr, WINHTTP_NO_REFERER,
                                           WINHTTP_DEFAULT_ACCEPT_TYPES, 0);
    std::wstring headers =
        L"X-Api-Key: " + std::wstring(API_KEY) + L"\r\nX-Actor: legacy.desktop\r\n";
    if (!key.empty())
        headers += L"Idempotency-Key: " + key + L"\r\n";
    if (!body.empty())
        headers += L"Content-Type: application/json\r\n";
    BOOL ok = WinHttpSendRequest(request, headers.c_str(), (DWORD)-1L,
                                 body.empty() ? WINHTTP_NO_REQUEST_DATA : (LPVOID)body.data(),
                                 (DWORD)body.size(), (DWORD)body.size(), 0) &&
              WinHttpReceiveResponse(request, nullptr);
    std::string all;
    if (ok)
    {
        DWORD size = 0;
        do
        {
            WinHttpQueryDataAvailable(request, &size);
            if (!size)
                break;
            std::string part(size, 0);
            DWORD got = 0;
            WinHttpReadData(request, &part[0], size, &got);
            part.resize(got);
            all += part;
        } while (size);
    }
    else
        all = "ERROR: API unavailable (" + std::to_string(GetLastError()) + ")";
    WinHttpCloseHandle(request);
    WinHttpCloseHandle(connect);
    WinHttpCloseHandle(session);
    return Wide(all);
}
static std::wstring Text(HWND h)
{
    int n = GetWindowTextLength(h);
    if (!n)
        return L"";
    std::wstring x(n + 1, 0);
    GetWindowText(h, &x[0], n + 1);
    x.resize(n);
    return x;
}
static std::wstring NewGuid()
{
    GUID g;
    CoCreateGuid(&g);
    wchar_t x[64];
    StringFromGUID2(g, x, 64);
    std::wstring s = x;
    return s.substr(1, s.size() - 2);
}
static int JsonInt(const std::wstring &j, const wchar_t *name)
{
    std::wstring k = L"\"" + std::wstring(name) + L"\":";
    auto p = j.find(k);
    return p == std::wstring::npos ? 0 : _wtoi(j.c_str() + p + k.size());
}
static bool JsonBool(const std::wstring &j, const wchar_t *name)
{
    std::wstring k = L"\"" + std::wstring(name) + L"\":";
    auto p = j.find(k);
    return p != std::wstring::npos && j.compare(p + k.size(), 4, L"true") == 0;
}
static std::wstring JsonString(const std::wstring &j, const wchar_t *name)
{
    std::wstring k = L"\"" + std::wstring(name) + L"\":\"";
    auto p = j.find(k);
    if (p == std::wstring::npos)
        return L"";
    p += k.size();
    auto e = j.find(L'\"', p);
    return j.substr(p, e - p);
}
static std::wstring PrettyJson(const std::wstring &json)
{
    auto first = json.find_first_not_of(L" \r\n\t");
    if (first == std::wstring::npos || (json[first] != L'{' && json[first] != L'['))
        return json;
    std::wstring result;
    int indent = 0;
    bool inString = false, escaped = false;
    for (wchar_t c : json)
    {
        if (inString)
        {
            result += c;
            if (escaped)
                escaped = false;
            else if (c == L'\\')
                escaped = true;
            else if (c == L'\"')
                inString = false;
            continue;
        }
        if (c == L'\"')
        {
            inString = true;
            result += c;
            continue;
        }
        if (c == L'{' || c == L'[')
        {
            result += c;
            result += L"\r\n";
            indent++;
            result.append(indent * 2, L' ');
        }
        else if (c == L'}' || c == L']')
        {
            result += L"\r\n";
            if (indent > 0)
                indent--;
            result.append(indent * 2, L' ');
            result += c;
        }
        else if (c == L',')
        {
            result += c;
            result += L"\r\n";
            result.append(indent * 2, L' ');
        }
        else if (c == L':')
        {
            result += L": ";
        }
        else if (c != L'\r' && c != L'\n' && c != L'\t' && c != L' ')
            result += c;
    }
    return result;
}
static void SetOutput(const std::wstring &s)
{
    SetWindowText(outputBox, PrettyJson(s).c_str());
}
static void Checkout()
{
    auto member = Text(memberBox), tool = Text(toolBox), due = Text(dueBox);
    if (member.empty() || tool.empty() || due.empty())
    {
        MessageBox(nullptr, L"Member, tool, and due date are required.", L"Validation",
                   MB_ICONWARNING);
        return;
    }
    auto m = Http(L"GET", L"/api/v1/members/" + member);
    auto tier = JsonString(m, L"tier");
    if (!IsEligibleForCheckout(JsonBool(m, L"active"), JsonBool(m, L"hasOverdueLoan"),
                               JsonInt(m, L"openLoans"), tier.c_str()))
    {
        MessageBox(nullptr,
                   L"Native business rules say this member is not eligible. The database will "
                   L"independently enforce the rule.",
                   L"Eligibility", MB_ICONWARNING);
        return;
    }
    std::string body = "{\"toolId\":" + Utf8(tool) + ",\"memberId\":" + Utf8(member) +
                       ",\"dueOn\":\"" + Utf8(due) + "\"}";
    if (MessageBox(nullptr, L"Complete this checkout?", L"Confirm", MB_YESNO | MB_ICONQUESTION) ==
        IDYES)
        SetOutput(Http(L"POST", L"/api/v1/checkouts", body, NewGuid()));
}
static void ReturnTool()
{
    auto loan = Text(loanBox);
    if (loan.empty() || loan.find_first_not_of(L"0123456789") != std::wstring::npos ||
        _wtoi64(loan.c_str()) <= 0)
    {
        MessageBox(nullptr, L"Enter a valid positive Loan ID.", L"Validation", MB_ICONWARNING);
        return;
    }
    std::string body = "{\"loanId\":" + Utf8(loan) + "}";
    if (MessageBox(nullptr, L"Return this tool?", L"Confirm return", MB_YESNO | MB_ICONQUESTION) ==
        IDYES)
        SetOutput(Http(L"POST", L"/api/v1/returns", body, NewGuid()));
}
static LRESULT CALLBACK WindowProc(HWND h, UINT msg, WPARAM w, LPARAM l)
{
    if (msg == WM_CREATE)
    {
        CreateWindow(L"STATIC", L"Member ID", WS_CHILD | WS_VISIBLE, 15, 15, 80, 20, h, 0, 0, 0);
        memberBox = CreateWindow(L"EDIT", L"1", WS_CHILD | WS_VISIBLE | WS_BORDER, 100, 12, 70, 24,
                                 h, (HMENU)ID_MEMBER, 0, 0);
        CreateWindow(L"BUTTON", L"Load Member", WS_CHILD | WS_VISIBLE, 180, 11, 100, 26, h,
                     (HMENU)ID_LOAD_MEMBER, 0, 0);
        CreateWindow(L"STATIC", L"Tool ID", WS_CHILD | WS_VISIBLE, 15, 52, 80, 20, h, 0, 0, 0);
        toolBox = CreateWindow(L"EDIT", L"1", WS_CHILD | WS_VISIBLE | WS_BORDER, 100, 49, 70, 24, h,
                               (HMENU)ID_TOOL, 0, 0);
        CreateWindow(L"STATIC", L"Due (YYYY-MM-DD)", WS_CHILD | WS_VISIBLE, 300, 15, 130, 20, h, 0,
                     0, 0);
        dueBox = CreateWindow(L"EDIT", L"2026-08-29", WS_CHILD | WS_VISIBLE | WS_BORDER, 435, 12,
                              110, 24, h, (HMENU)ID_DUE, 0, 0);
        CreateWindow(L"BUTTON", L"Check Out", WS_CHILD | WS_VISIBLE, 300, 48, 110, 28, h,
                     (HMENU)ID_CHECKOUT, 0, 0);
        CreateWindow(L"BUTTON", L"Refresh Tools", WS_CHILD | WS_VISIBLE, 425, 48, 120, 28, h,
                     (HMENU)ID_REFRESH, 0, 0);
        CreateWindow(L"STATIC", L"Loan ID", WS_CHILD | WS_VISIBLE, 570, 15, 65, 20, h, 0, 0, 0);
        loanBox = CreateWindow(L"EDIT", L"", WS_CHILD | WS_VISIBLE | WS_BORDER, 635, 12, 70, 24, h,
                               (HMENU)ID_LOAN, 0, 0);
        CreateWindow(L"BUTTON", L"Return Tool", WS_CHILD | WS_VISIBLE, 570, 48, 135, 28, h,
                     (HMENU)ID_RETURN, 0, 0);
        outputBox = CreateWindow(L"EDIT", L"Click Refresh Tools to begin.",
                                 WS_CHILD | WS_VISIBLE | WS_BORDER | ES_MULTILINE | ES_AUTOVSCROLL |
                                     WS_VSCROLL | WS_HSCROLL | ES_AUTOHSCROLL | ES_READONLY,
                                 15, 90, 750, 400, h, (HMENU)ID_OUTPUT, 0, 0);
        SendMessage(outputBox, WM_SETFONT, (WPARAM)GetStockObject(ANSI_FIXED_FONT), TRUE);
        return 0;
    }
    if (msg == WM_COMMAND)
    {
        switch (LOWORD(w))
        {
        case ID_REFRESH:
            SetOutput(Http(L"GET", L"/api/v1/tools"));
            break;
        case ID_LOAD_MEMBER:
            SetOutput(Http(L"GET", L"/api/v1/members/" + Text(memberBox)));
            break;
        case ID_CHECKOUT:
            Checkout();
            break;
        case ID_RETURN:
            ReturnTool();
            break;
        }
        return 0;
    }
    if (msg == WM_DESTROY)
    {
        PostQuitMessage(0);
        return 0;
    }
    return DefWindowProc(h, msg, w, l);
}
int WINAPI wWinMain(HINSTANCE instance, HINSTANCE, LPWSTR, int show)
{
    CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    WNDCLASS wc = {};
    wc.lpfnWndProc = WindowProc;
    wc.hInstance = instance;
    wc.lpszClassName = L"ToolLendingLegacyForm";
    wc.hbrBackground = (HBRUSH)(COLOR_BTNFACE + 1);
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
    RegisterClass(&wc);
    HWND h = CreateWindow(wc.lpszClassName, L"Community Tool Lending - Legacy Client",
                          WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, 800, 550, nullptr,
                          nullptr, instance, nullptr);
    ShowWindow(h, show);
    MSG msg;
    while (GetMessage(&msg, nullptr, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }
    CoUninitialize();
    return 0;
}
