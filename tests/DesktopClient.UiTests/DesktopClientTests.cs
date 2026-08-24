using System;
using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using NUnit.Framework;

namespace ToolLending.DesktopClient.UiTests;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class DesktopClientTests
{
    private const string MemberId = "106";
    private const string ToolId = "105";
    private const string DueDate = "107";
    private const string LoanId = "108";
    private const string CheckOut = "103";
    private const string RefreshTools = "101";
    private const string ReturnTool = "104";

    private Application? application;
    private UIA3Automation? automation;
    private Window? window;

    [SetUp]
    public void StartClient()
    {
        var executable = Environment.GetEnvironmentVariable("TOOL_LENDING_UI_EXE");
        Assert.That(executable, Is.Not.Null.And.Not.Empty,
            "Run UI tests through scripts/Run-UiTests.ps1 so the client path is configured.");
        Assert.That(File.Exists(executable), Is.True, $"Desktop client was not found: {executable}");

        application = Application.Launch(executable!);
        automation = new UIA3Automation();
        window = WaitForMainWindow(application, automation);
    }

    [TearDown]
    public void StopClient()
    {
        if (application is { HasExited: false })
        {
            application.Close();
        }

        automation?.Dispose();
        application?.Dispose();
    }

    [Test]
    public void ExposesStableAutomationIdsForInteractiveControls()
    {
        AssertControl(MemberId, "Member ID");
        AssertControl(ToolId, "Tool ID");
        AssertControl(DueDate, "Due date");
        AssertControl(LoanId, "Loan ID");
        AssertControl(CheckOut, "Check Out");
        AssertControl(RefreshTools, "Refresh Tools");
        AssertControl(ReturnTool, "Return Tool");
    }

    [Test]
    public void CheckoutWithMissingMemberShowsValidationMessage()
    {
        Find(MemberId).AsTextBox().Text = string.Empty;
        Find(CheckOut).AsButton().Invoke();

        var dialog = WaitForModalWindow();
        Assert.That(dialog.Title, Is.EqualTo("Validation"));
        Assert.That(
            dialog.FindFirstDescendant(cf =>
                cf.ByName("Member, tool, and due date are required.")),
            Is.Not.Null);

        dialog.FindFirstDescendant(cf => cf.ByName("OK"))?.AsButton().Invoke();
    }

    private AutomationElement Find(string automationId)
    {
        return window!.FindFirstDescendant(cf => cf.ByAutomationId(automationId))
            ?? throw new AssertionException($"Control AutomationId {automationId} was not found.");
    }

    private void AssertControl(string automationId, string description)
    {
        Assert.That(Find(automationId), Is.Not.Null, $"{description} control is not exposed.");
    }

    private Window WaitForModalWindow()
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            // The legacy client creates MessageBox dialogs without an owner HWND, so Windows
            // exposes them as top-level desktop windows rather than children of the main window.
            var dialog = automation!.GetDesktop()
                .FindFirstChild(cf => cf.ByName("Validation"))
                ?.AsWindow();
            if (dialog is not null)
            {
                return dialog;
            }

            Thread.Sleep(100);
        }

        throw new AssertionException("Expected validation dialog did not appear.");
    }

    private static Window WaitForMainWindow(Application app, UIA3Automation uiAutomation)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var mainWindow = app.GetMainWindow(uiAutomation);
            if (mainWindow is not null)
            {
                return mainWindow;
            }

            Thread.Sleep(100);
        }

        throw new AssertionException("Desktop client window did not appear.");
    }
}
