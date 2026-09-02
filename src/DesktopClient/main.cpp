#define UNICODE
#define _UNICODE
#include <windows.h>
#include <commctrl.h>
#include <winhttp.h>
#include <fstream>
#include <string>
#include <sstream>
#include <iomanip>
#include <vector>
#include "../NativeRules/NativeRules.h"
#include "ClientTransport.h"
#pragma comment(lib, "winhttp.lib")
#pragma comment(lib, "comctl32.lib")

enum
{
    // Win32 exposes these control IDs to Microsoft UI Automation as AutomationId values.
    // Keep every value explicit and stable so FlaUI tests do not break when controls are added.
    ID_REFRESH = 101,
    ID_LOAD_MEMBER = 102,
    ID_CHECKOUT = 103,
    ID_RETURN = 104,
    ID_TOOL = 105,
    ID_MEMBER = 106,
    ID_DUE = 107,
    ID_LOAN = 108,
    ID_OUTPUT = 109,
    ID_CREDENTIAL_MODE = 110,
    ID_TABS = 111,

    ID_USER_NAME = 120,
    ID_USER_TIER = 121,
    ID_USER_ACTIVE = 122,
    ID_ADD_USER = 123,
    ID_USER_RESULT = 124,

    ID_ASSET_TAG = 130,
    ID_TOOL_NAME = 131,
    ID_LATE_FEE = 132,
    ID_ADD_TOOL = 133,
    ID_TOOL_RESULT = 134,

    // Give visible labels their own stable IDs so accessibility tools and UI tests can identify
    // them independently of display position. Keeping each label immediately before its input
    // also gives Win32 accessibility clients the context needed to associate the two controls.
    ID_MEMBER_LABEL = 201,
    ID_TOOL_LABEL = 202,
    ID_DUE_LABEL = 203,
    ID_LOAN_LABEL = 204,
    ID_USER_NAME_LABEL = 220,
    ID_USER_TIER_LABEL = 221,
    ID_USER_ID_LABEL = 222,
    ID_ASSET_TAG_LABEL = 230,
    ID_TOOL_NAME_LABEL = 231,
    ID_LATE_FEE_LABEL = 232,
    ID_TOOL_ID_LABEL = 233
};
static HWND outputBox, toolBox, memberBox, dueBox, loanBox;
static HWND userNameBox, userTierBox, userActiveBox, userResultBox;
static HWND assetTagBox, toolNameBox, lateFeeBox, toolResultBox;
static std::vector<HWND> lendingControls, userControls, addToolControls;
static std::wstring apiKey = L"demo-local-key";
static std::wstring credentialMode = L"Legacy shared credential: built-in demo fallback";
static ClientEndpointConfiguration endpointConfiguration;
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
static std::wstring EnvironmentValue(const wchar_t *name)
{
    DWORD size = GetEnvironmentVariable(name, nullptr, 0);
    if (!size)
        return L"";
    std::wstring value(size, L'\0');
    GetEnvironmentVariable(name, &value[0], size);
    value.resize(size - 1);
    return value;
}
static bool LoadLegacyCredential(std::wstring &error)
{
    const auto path = EnvironmentValue(L"TOOL_LENDING_LEGACY_CREDENTIAL_FILE");
    if (path.empty())
        return true;

    // A UNC path models the current practice-wide SMB share. A local path is deliberately also
    // supported so development and automated tests can characterize the behavior without creating
    // or exposing a real network share.
    std::ifstream file(Utf8(path), std::ios::binary);
    if (!file)
    {
        error = L"The configured Legacy shared credential file could not be opened:\r\n" + path;
        return false;
    }

    std::string value((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
    if (value.compare(0, 3, "\xEF\xBB\xBF") == 0)
        value.erase(0, 3);
    auto first = value.find_first_not_of(" \t\r\n");
    auto last = value.find_last_not_of(" \t\r\n");
    if (first == std::string::npos)
    {
        error = L"The configured Legacy shared credential file is empty:\r\n" + path;
        return false;
    }

    apiKey = Wide(value.substr(first, last - first + 1));
    credentialMode = L"Legacy shared credential file: " + path;
    return true;
}
// Sends current product calls to the configured Legacy endpoint. Connected endpoint selection is
// added only after a current service capability is cached; keyed ambiguous writes reuse their key.
static std::wstring Http(const wchar_t *verb, const std::wstring &path,
                         const std::string &body = "", const std::wstring &key = L"")
{
    return FormatClientHttpResult(SendClientHttp(
        endpointConfiguration, endpointConfiguration.legacy, verb, path, apiKey, body, key));
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
static std::string JsonEscape(const std::wstring &value)
{
    std::string result;
    for (char c : Utf8(value))
    {
        switch (c)
        {
        case '\\':
            result += "\\\\";
            break;
        case '"':
            result += "\\\"";
            break;
        case '\r':
            result += "\\r";
            break;
        case '\n':
            result += "\\n";
            break;
        case '\t':
            result += "\\t";
            break;
        default:
            result += c;
        }
    }
    return result;
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
static HWND ScreenControl(std::vector<HWND> &screen, const wchar_t *className, const wchar_t *text,
                          DWORD style, int x, int y, int width, int height, HWND parent, int id)
{
    auto control =
        CreateWindow(className, text, style, x, y, width, height, parent, (HMENU)id, 0, 0);
    screen.push_back(control);
    return control;
}
static void ShowScreen(int selected)
{
    const std::vector<HWND> *screens[] = {&lendingControls, &userControls, &addToolControls};
    for (int i = 0; i < 3; i++)
        for (auto control : *screens[i])
            ShowWindow(control, i == selected ? SW_SHOW : SW_HIDE);
}
static void AddUser()
{
    auto name = Text(userNameBox);
    if (name.empty())
    {
        MessageBox(nullptr, L"User name is required.", L"Validation", MB_ICONWARNING);
        return;
    }

    int selectedTier = (int)SendMessage(userTierBox, CB_GETCURSEL, 0, 0);
    wchar_t tier[32] = {};
    SendMessage(userTierBox, CB_GETLBTEXT, selectedTier, (LPARAM)tier);
    bool active = SendMessage(userActiveBox, BM_GETCHECK, 0, 0) == BST_CHECKED;
    std::string body = "{\"displayName\":\"" + JsonEscape(name) + "\",\"tier\":\"" + Utf8(tier) +
                       "\",\"active\":" + (active ? "true" : "false") + "}";
    SetWindowText(userResultBox,
                  PrettyJson(Http(L"POST", L"/api/v1/members", body, NewGuid())).c_str());
}
static void AddTool()
{
    auto assetTag = Text(assetTagBox), name = Text(toolNameBox), fee = Text(lateFeeBox);
    wchar_t *end = nullptr;
    double parsedFee = wcstod(fee.c_str(), &end);
    if (assetTag.empty() || name.empty() || fee.empty() || !end || *end != L'\0' || parsedFee < 0)
    {
        MessageBox(nullptr, L"Asset tag, tool name, and a non-negative late fee are required.",
                   L"Validation", MB_ICONWARNING);
        return;
    }

    std::string body = "{\"assetTag\":\"" + JsonEscape(assetTag) + "\",\"displayName\":\"" +
                       JsonEscape(name) + "\",\"dailyLateFee\":" + Utf8(fee) + "}";
    SetWindowText(toolResultBox,
                  PrettyJson(Http(L"POST", L"/api/v1/tools", body, NewGuid())).c_str());
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
        auto tabs = CreateWindow(WC_TABCONTROL, L"", WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS, 8, 8,
                                 770, 515, h, (HMENU)ID_TABS, 0, 0);
        TCITEM tab = {};
        tab.mask = TCIF_TEXT;
        const wchar_t *tabNames[] = {L"Lending", L"Add user", L"Add tool"};
        for (int i = 0; i < 3; i++)
        {
            tab.pszText = const_cast<wchar_t *>(tabNames[i]);
            TabCtrl_InsertItem(tabs, i, &tab);
        }

        // These visible labels are intentional accessibility metadata as well as UI text. FlaUI
        // and screen readers use them to describe the adjacent edit controls without relying on
        // screen coordinates, which makes validation stable across DPI and layout changes.
        ScreenControl(lendingControls, L"STATIC", L"Member ID", WS_CHILD | WS_VISIBLE, 22, 48, 80,
                      20, h, ID_MEMBER_LABEL);
        memberBox = ScreenControl(lendingControls, L"EDIT", L"1", WS_CHILD | WS_VISIBLE | WS_BORDER,
                                  107, 45, 70, 24, h, ID_MEMBER);
        ScreenControl(lendingControls, L"BUTTON", L"Load Member", WS_CHILD | WS_VISIBLE, 187, 44,
                      100, 26, h, ID_LOAD_MEMBER);
        ScreenControl(lendingControls, L"STATIC", L"Tool ID", WS_CHILD | WS_VISIBLE, 22, 85, 80, 20,
                      h, ID_TOOL_LABEL);
        toolBox = ScreenControl(lendingControls, L"EDIT", L"1", WS_CHILD | WS_VISIBLE | WS_BORDER,
                                107, 82, 70, 24, h, ID_TOOL);
        ScreenControl(lendingControls, L"STATIC", L"Due (YYYY-MM-DD)", WS_CHILD | WS_VISIBLE, 307,
                      48, 130, 20, h, ID_DUE_LABEL);
        dueBox = ScreenControl(lendingControls, L"EDIT", L"2026-08-29",
                               WS_CHILD | WS_VISIBLE | WS_BORDER, 442, 45, 110, 24, h, ID_DUE);
        ScreenControl(lendingControls, L"BUTTON", L"Check Out", WS_CHILD | WS_VISIBLE, 307, 81, 110,
                      28, h, ID_CHECKOUT);
        ScreenControl(lendingControls, L"BUTTON", L"Refresh Tools", WS_CHILD | WS_VISIBLE, 432, 81,
                      120, 28, h, ID_REFRESH);
        ScreenControl(lendingControls, L"STATIC", L"Loan ID", WS_CHILD | WS_VISIBLE, 577, 48, 65,
                      20, h, ID_LOAN_LABEL);
        loanBox = ScreenControl(lendingControls, L"EDIT", L"", WS_CHILD | WS_VISIBLE | WS_BORDER,
                                642, 45, 70, 24, h, ID_LOAN);
        ScreenControl(lendingControls, L"BUTTON", L"Return Tool", WS_CHILD | WS_VISIBLE, 577, 81,
                      135, 28, h, ID_RETURN);
        outputBox =
            ScreenControl(lendingControls, L"EDIT", L"Click Refresh Tools to begin.",
                          WS_CHILD | WS_VISIBLE | WS_BORDER | ES_MULTILINE | ES_AUTOVSCROLL |
                              WS_VSCROLL | WS_HSCROLL | ES_AUTOHSCROLL | ES_READONLY,
                          22, 123, 740, 330, h, ID_OUTPUT);
        SendMessage(outputBox, WM_SETFONT, (WPARAM)GetStockObject(ANSI_FIXED_FONT), TRUE);
        // This label makes the deliberately weak Legacy trust model visible and testable. It shows
        // where the practice-shared credential came from, but never renders the credential value.
        ScreenControl(lendingControls, L"STATIC", credentialMode.c_str(),
                      WS_CHILD | WS_VISIBLE | SS_PATHELLIPSIS, 22, 465, 740, 20, h,
                      ID_CREDENTIAL_MODE);

        ScreenControl(userControls, L"STATIC", L"Add a user", WS_CHILD | WS_VISIBLE, 35, 55, 200,
                      24, h, 0);
        ScreenControl(userControls, L"STATIC", L"Member ID", WS_CHILD | WS_VISIBLE, 35, 100, 100,
                      20, h, ID_USER_ID_LABEL);
        ScreenControl(userControls, L"STATIC", L"Assigned automatically when saved",
                      WS_CHILD | WS_VISIBLE, 165, 100, 260, 20, h, 0);
        ScreenControl(userControls, L"STATIC", L"Display name", WS_CHILD | WS_VISIBLE, 35, 140, 110,
                      20, h, ID_USER_NAME_LABEL);
        userNameBox = ScreenControl(userControls, L"EDIT", L"",
                                    WS_CHILD | WS_VISIBLE | WS_BORDER | ES_AUTOHSCROLL, 165, 137,
                                    300, 24, h, ID_USER_NAME);
        ScreenControl(userControls, L"STATIC", L"Membership tier", WS_CHILD | WS_VISIBLE, 35, 180,
                      120, 20, h, ID_USER_TIER_LABEL);
        userTierBox =
            ScreenControl(userControls, WC_COMBOBOX, L"",
                          WS_CHILD | WS_VISIBLE | WS_BORDER | CBS_DROPDOWNLIST | WS_VSCROLL, 165,
                          177, 180, 140, h, ID_USER_TIER);
        SendMessage(userTierBox, CB_ADDSTRING, 0, (LPARAM)L"STANDARD");
        SendMessage(userTierBox, CB_ADDSTRING, 0, (LPARAM)L"SUPPORTER");
        SendMessage(userTierBox, CB_ADDSTRING, 0, (LPARAM)L"STAFF");
        SendMessage(userTierBox, CB_SETCURSEL, 0, 0);
        userActiveBox = ScreenControl(userControls, L"BUTTON", L"Active member",
                                      WS_CHILD | WS_VISIBLE | BS_AUTOCHECKBOX, 165, 220, 180, 24, h,
                                      ID_USER_ACTIVE);
        SendMessage(userActiveBox, BM_SETCHECK, BST_CHECKED, 0);
        ScreenControl(userControls, L"BUTTON", L"Add user",
                      WS_CHILD | WS_VISIBLE | BS_DEFPUSHBUTTON, 165, 265, 120, 30, h, ID_ADD_USER);
        userResultBox =
            ScreenControl(userControls, L"EDIT", L"The generated member ID will appear here.",
                          WS_CHILD | WS_VISIBLE | WS_BORDER | ES_MULTILINE | ES_READONLY, 35, 325,
                          700, 120, h, ID_USER_RESULT);

        ScreenControl(addToolControls, L"STATIC", L"Add a tool", WS_CHILD | WS_VISIBLE, 35, 55, 200,
                      24, h, 0);
        ScreenControl(addToolControls, L"STATIC", L"Tool ID", WS_CHILD | WS_VISIBLE, 35, 100, 100,
                      20, h, ID_TOOL_ID_LABEL);
        ScreenControl(addToolControls, L"STATIC", L"Assigned automatically when saved",
                      WS_CHILD | WS_VISIBLE, 165, 100, 260, 20, h, 0);
        ScreenControl(addToolControls, L"STATIC", L"Asset tag", WS_CHILD | WS_VISIBLE, 35, 140, 110,
                      20, h, ID_ASSET_TAG_LABEL);
        assetTagBox = ScreenControl(addToolControls, L"EDIT", L"",
                                    WS_CHILD | WS_VISIBLE | WS_BORDER | ES_AUTOHSCROLL, 165, 137,
                                    220, 24, h, ID_ASSET_TAG);
        ScreenControl(addToolControls, L"STATIC", L"Tool name", WS_CHILD | WS_VISIBLE, 35, 180, 110,
                      20, h, ID_TOOL_NAME_LABEL);
        toolNameBox = ScreenControl(addToolControls, L"EDIT", L"",
                                    WS_CHILD | WS_VISIBLE | WS_BORDER | ES_AUTOHSCROLL, 165, 177,
                                    300, 24, h, ID_TOOL_NAME);
        ScreenControl(addToolControls, L"STATIC", L"Daily late fee", WS_CHILD | WS_VISIBLE, 35, 220,
                      110, 20, h, ID_LATE_FEE_LABEL);
        lateFeeBox = ScreenControl(addToolControls, L"EDIT", L"0.00",
                                   WS_CHILD | WS_VISIBLE | WS_BORDER | ES_AUTOHSCROLL, 165, 217,
                                   100, 24, h, ID_LATE_FEE);
        ScreenControl(addToolControls, L"BUTTON", L"Add tool",
                      WS_CHILD | WS_VISIBLE | BS_DEFPUSHBUTTON, 165, 265, 120, 30, h, ID_ADD_TOOL);
        toolResultBox =
            ScreenControl(addToolControls, L"EDIT", L"The generated tool ID will appear here.",
                          WS_CHILD | WS_VISIBLE | WS_BORDER | ES_MULTILINE | ES_READONLY, 35, 325,
                          700, 120, h, ID_TOOL_RESULT);

        ShowScreen(0);
        return 0;
    }
    if (msg == WM_NOTIFY && ((LPNMHDR)l)->idFrom == ID_TABS && ((LPNMHDR)l)->code == TCN_SELCHANGE)
    {
        ShowScreen(TabCtrl_GetCurSel(((LPNMHDR)l)->hwndFrom));
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
        case ID_ADD_USER:
            AddUser();
            break;
        case ID_ADD_TOOL:
            AddTool();
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
// Validates endpoint and Legacy credential configuration before creating the desktop window.
int WINAPI wWinMain(HINSTANCE instance, HINSTANCE, LPWSTR, int show)
{
    std::wstring endpointError;
    if (!LoadClientEndpointConfiguration(endpointConfiguration, endpointError))
    {
        MessageBox(nullptr, endpointError.c_str(), L"Endpoint configuration error", MB_ICONERROR);
        return 1;
    }
    std::wstring credentialError;
    if (!LoadLegacyCredential(credentialError))
    {
        MessageBox(nullptr, credentialError.c_str(), L"Legacy credential error", MB_ICONERROR);
        return 2;
    }

    CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    INITCOMMONCONTROLSEX controls = {sizeof(INITCOMMONCONTROLSEX), ICC_TAB_CLASSES};
    InitCommonControlsEx(&controls);
    WNDCLASS wc = {};
    wc.lpfnWndProc = WindowProc;
    wc.hInstance = instance;
    wc.lpszClassName = L"ToolLendingLegacyForm";
    wc.hbrBackground = (HBRUSH)(COLOR_BTNFACE + 1);
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
    RegisterClass(&wc);
    HWND h = CreateWindow(wc.lpszClassName, L"Community Tool Lending - Legacy Client",
                          WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, 800, 570, nullptr,
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
