using System;
using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using NUnit.Framework;

namespace ToolLending.DesktopClient.UiTests;

// Protects the visible desktop contract while checkout routing changes underneath the Win32 UI.
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
    private const string CredentialMode = "110";
    private const string Tabs = "111";
    private const string UserName = "120";
    private const string UserTier = "121";
    private const string UserActive = "122";
    private const string AddUser = "123";
    private const string UserResult = "124";
    private const string AssetTag = "130";
    private const string ToolName = "131";
    private const string LateFee = "132";
    private const string AddTool = "133";
    private const string ToolResult = "134";

    private Application? application;
    private UIA3Automation? automation;
    private Window? window;

    [SetUp]
    public void StartClient()
    {
        var executable = Environment.GetEnvironmentVariable("TOOL_LENDING_UI_EXE");
        Assert.That(
            executable,
            Is.Not.Null.And.Not.Empty,
            "Run UI tests through scripts/Run-UiTests.ps1 so the client path is configured."
        );
        Assert.That(
            File.Exists(executable),
            Is.True,
            $"Desktop client was not found: {executable}"
        );

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
        AssertControl(CredentialMode, "Legacy credential mode");
        AssertControl(Tabs, "Main tabs");
    }

    [Test]
    public void AddUserTabExposesGeneratedIdWorkflow()
    {
        SelectTab("Add user");

        AssertControl(UserName, "User name");
        AssertControl(UserTier, "Membership tier");
        AssertControl(UserActive, "Active member");
        AssertControl(AddUser, "Add user");
        Assert.That(Find(UserResult).AsTextBox().Text, Does.Contain("generated member ID"));
        Assert.That(
            window!.FindFirstDescendant(cf => cf.ByName("Assigned automatically when saved")),
            Is.Not.Null
        );
    }

    [Test]
    public void AddToolTabExposesGeneratedIdWorkflow()
    {
        SelectTab("Add tool");

        AssertControl(AssetTag, "Asset tag");
        AssertControl(ToolName, "Tool name");
        AssertControl(LateFee, "Daily late fee");
        AssertControl(AddTool, "Add tool");
        Assert.That(Find(ToolResult).AsTextBox().Text, Does.Contain("generated tool ID"));
    }

    [Test]
    public void AddUserWithoutNameShowsValidationMessage()
    {
        SelectTab("Add user");
        Find(UserName).AsTextBox().Text = string.Empty;
        Find(AddUser).AsButton().Invoke();

        var dialog = WaitForModalWindow();
        Assert.That(
            dialog.FindFirstDescendant(cf => cf.ByName("User name is required.")),
            Is.Not.Null
        );
        dialog.FindFirstDescendant(cf => cf.ByName("OK"))?.AsButton().Invoke();
    }

    [Test]
    public void DisplaysSharedCredentialSourceWithoutExposingCredential()
    {
        var configuredPath = Environment.GetEnvironmentVariable(
            "TOOL_LENDING_LEGACY_CREDENTIAL_FILE"
        );
        var secret = Environment.GetEnvironmentVariable("TOOL_LENDING_UI_TEST_SHARED_KEY");
        var label = Find(CredentialMode).Name;

        Assert.That(configuredPath, Is.Not.Null.And.Not.Empty);
        Assert.That(secret, Is.Not.Null.And.Not.Empty);
        Assert.That(label, Does.StartWith("Legacy shared credential file:"));
        Assert.That(label, Does.Contain(configuredPath));
        Assert.That(
            label,
            Does.Not.Contain(secret),
            "The UI may display the credential source, but must never display its value."
        );
    }

    [Test]
    public void CheckoutWithMissingMemberShowsValidationMessage()
    {
        Find(MemberId).AsTextBox().Text = string.Empty;
        Find(CheckOut).AsButton().Invoke();

        var dialog = WaitForModalWindow();
        Assert.That(dialog.Title, Is.EqualTo("Validation"));
        Assert.That(
            dialog.FindFirstDescendant(cf => cf.ByName("Member, tool, and due date are required.")),
            Is.Not.Null
        );

        dialog.FindFirstDescendant(cf => cf.ByName("OK"))?.AsButton().Invoke();
    }

    // Confirms malformed identifiers are stopped before either rule implementation or HTTP path.
    [Test]
    public void CheckoutWithInvalidIdentifierShowsValidationMessage()
    {
        Find(MemberId).AsTextBox().Text = "not-an-id";
        Find(ToolId).AsTextBox().Text = "1";
        Find(DueDate).AsTextBox().Text = "2026-09-05";
        Find(CheckOut).AsButton().Invoke();

        var dialog = WaitForModalWindow();
        Assert.That(
            dialog.FindFirstDescendant(cf =>
                cf.ByName("Enter positive Member and Tool IDs and a due date in YYYY-MM-DD format.")
            ),
            Is.Not.Null
        );
        dialog.FindFirstDescendant(cf => cf.ByName("OK"))?.AsButton().Invoke();
    }

    // Confirms the Legacy/compare migration retains operator confirmation and that cancellation
    // produces no success output or checkout request. The database remains the final writer.
    [Test]
    public void CheckoutCancellationDoesNotReportSuccess()
    {
        var outputBefore = Find("109").AsTextBox().Text;
        Find(MemberId).AsTextBox().Text = "1";
        Find(ToolId).AsTextBox().Text = "1";
        Find(DueDate).AsTextBox().Text = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
        Find(CheckOut).AsButton().Invoke();

        var dialog = WaitForModalWindow("Confirm");
        Assert.That(
            dialog.FindFirstDescendant(cf => cf.ByName("Complete this checkout?")),
            Is.Not.Null
        );
        dialog.FindFirstDescendant(cf => cf.ByName("No"))?.AsButton().Invoke();

        Assert.That(Find("109").AsTextBox().Text, Is.EqualTo(outputBefore));
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

    private void SelectTab(string name)
    {
        var tab =
            Find(Tabs).FindFirstDescendant(cf => cf.ByName(name))?.AsTabItem()
            ?? throw new AssertionException($"Tab '{name}' was not found.");
        tab.Select();
    }

    // Waits for an unowned Win32 message box by title because it appears under the desktop root.
    private Window WaitForModalWindow(string title = "Validation")
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            // The legacy client creates MessageBox dialogs without an owner HWND, so Windows
            // exposes them as top-level desktop windows rather than children of the main window.
            var dialog = automation!
                .GetDesktop()
                .FindFirstChild(cf => cf.ByName(title))
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

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class LegacyCredentialStartupTests
{
    [Test]
    public void MissingConfiguredCredentialFileFailsClosed()
    {
        var executable = Environment.GetEnvironmentVariable("TOOL_LENDING_UI_EXE");
        var originalPath = Environment.GetEnvironmentVariable(
            "TOOL_LENDING_LEGACY_CREDENTIAL_FILE"
        );
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".credential");

        Assert.That(executable, Is.Not.Null.And.Not.Empty);
        Assert.That(File.Exists(missingPath), Is.False);

        Application? application = null;
        using var automation = new UIA3Automation();
        try
        {
            Environment.SetEnvironmentVariable("TOOL_LENDING_LEGACY_CREDENTIAL_FILE", missingPath);
            application = Application.Launch(executable!);

            var deadline = DateTime.UtcNow.AddSeconds(5);
            Window? dialog = null;
            while (DateTime.UtcNow < deadline && dialog is null)
            {
                dialog = automation
                    .GetDesktop()
                    .FindFirstChild(cf => cf.ByName("Legacy credential error"))
                    ?.AsWindow();
                Thread.Sleep(100);
            }

            Assert.That(dialog, Is.Not.Null, "The credential startup error did not appear.");
            Assert.That(
                dialog!.FindFirstDescendant(cf =>
                    cf.ByName(
                        $"The configured Legacy shared credential file could not be opened:\r\n{missingPath}"
                    )
                ),
                Is.Not.Null
            );
            dialog.FindFirstDescendant(cf => cf.ByName("OK"))?.AsButton().Invoke();

            var exitDeadline = DateTime.UtcNow.AddSeconds(2);
            while (DateTime.UtcNow < exitDeadline && !application.HasExited)
            {
                Thread.Sleep(50);
            }
            Assert.That(application.HasExited, Is.True);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TOOL_LENDING_LEGACY_CREDENTIAL_FILE", originalPath);
            if (application is { HasExited: false })
            {
                application.Kill();
            }
            application?.Dispose();
        }
    }
}
