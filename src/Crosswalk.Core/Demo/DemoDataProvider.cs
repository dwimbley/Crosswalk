using Crosswalk.Core.Cache;
using Crosswalk.Core.Configuration;
using Crosswalk.Core.Models;
using Crosswalk.Core.Sync;

namespace Crosswalk.Core.Demo;

/// <summary>
/// Provides fixture data for --demo mode: 4 fake sites with different emails, workflows,
/// a ticket on two boards, a duplicate key across sites, and one connection needing reauthorization.
/// </summary>
public static class DemoDataProvider
{
    private static readonly string[] DefaultPalette = ["#2563eb", "#7c3aed", "#ea580c", "#059669"];

    public static CrosswalkConfig GetDemoConfig() => new()
    {
        Version = 1,
        RefreshMinutes = 15,
        Connections =
        [
            new ConnectionConfig
            {
                Id = "harborline",
                DisplayName = "Harborline",
                BaseUrl = "https://harborline.atlassian.net",
                Email = "jsmith@demo-mode.org",
                Projects = ["CLM", "PLAT"],
                Boards =
                [
                    new BoardConfig { Id = 1, Name = "Claims Sprint Board" },
                    new BoardConfig { Id = 2, Name = "Platform Kanban" }
                ],
                Color = DefaultPalette[0],
                Enabled = true
            },
            new ConnectionConfig
            {
                Id = "quarrylabs",
                DisplayName = "Quarry Labs",
                BaseUrl = "https://quarrylabs.atlassian.net",
                Email = "jsmith@demo-mode.org",
                Projects = ["PLAT", "RPT"],
                Boards =
                [
                    new BoardConfig { Id = 10, Name = "Platform Sprint" },
                    new BoardConfig { Id = 11, Name = "Reporting Board" }
                ],
                Color = DefaultPalette[1],
                Enabled = true
            },
            new ConnectionConfig
            {
                Id = "meridianpay",
                DisplayName = "Meridian Pay",
                BaseUrl = "https://meridianpay.atlassian.net",
                Email = "jsmith@demo-mode.org",
                Projects = ["MOB", "API"],
                Boards =
                [
                    new BoardConfig { Id = 20, Name = "Mobile Sprint" },
                    new BoardConfig { Id = 21, Name = "API Kanban" }
                ],
                Color = DefaultPalette[2],
                Enabled = true
            },
            new ConnectionConfig
            {
                Id = "northbeam",
                DisplayName = "Northbeam",
                BaseUrl = "https://northbeam.atlassian.net",
                Email = "jsmith@demo-mode.org",
                Projects = ["OPS", "DATA"],
                Boards =
                [
                    new BoardConfig { Id = 30, Name = "Ops Kanban" },
                    new BoardConfig { Id = 31, Name = "Data Sprint" }
                ],
                Color = DefaultPalette[3],
                Enabled = true
            }
        ]
    };

    public static Dictionary<string, ConnectionStatus> GetDemoStatuses()
    {
        var now = DateTimeOffset.Now;
        return new Dictionary<string, ConnectionStatus>
        {
            ["harborline"] = new()
            {
                Id = "harborline",
                DisplayName = "Harborline",
                Color = DefaultPalette[0],
                Health = ConnectionHealth.Healthy,
                LastSuccessfulRefresh = now.AddMinutes(-19),
                TicketCount = 7,
                IdentityDisplayName = "John Smith",
                IdentityAccountId = "acc-h1",
                Email = "jsmith@demo-mode.org",
                BaseUrl = "https://harborline.atlassian.net",
                Projects = ["CLM", "PLAT"],
                Boards = [new BoardRef { Id = 1, Name = "Claims Sprint Board" }, new BoardRef { Id = 2, Name = "Platform Kanban" }],
                Enabled = true
            },
            ["quarrylabs"] = new()
            {
                Id = "quarrylabs",
                DisplayName = "Quarry Labs",
                Color = DefaultPalette[1],
                Health = ConnectionHealth.Healthy,
                LastSuccessfulRefresh = now.AddMinutes(-20),
                TicketCount = 5,
                IdentityDisplayName = "John Smith (Consultant)",
                IdentityAccountId = "acc-q1",
                Email = "jsmith@demo-mode.org",
                BaseUrl = "https://quarrylabs.atlassian.net",
                Projects = ["PLAT", "RPT"],
                Boards = [new BoardRef { Id = 10, Name = "Platform Sprint" }, new BoardRef { Id = 11, Name = "Reporting Board" }],
                Enabled = true
            },
            ["meridianpay"] = new()
            {
                Id = "meridianpay",
                DisplayName = "Meridian Pay",
                Color = DefaultPalette[2],
                Health = ConnectionHealth.NeedsReauthorization,
                LastSuccessfulRefresh = now.AddHours(-2),
                TicketCount = 4,
                Error = "Authentication failed: 401 Unauthorized. Please re-enter your API token.",
                IdentityDisplayName = "J. Smith",
                IdentityAccountId = "acc-m1",
                Email = "jsmith@demo-mode.org",
                BaseUrl = "https://meridianpay.atlassian.net",
                Projects = ["MOB", "API"],
                Boards = [new BoardRef { Id = 20, Name = "Mobile Sprint" }, new BoardRef { Id = 21, Name = "API Kanban" }],
                Enabled = true
            },
            ["northbeam"] = new()
            {
                Id = "northbeam",
                DisplayName = "Northbeam",
                Color = DefaultPalette[3],
                Health = ConnectionHealth.Healthy,
                LastSuccessfulRefresh = now.AddMinutes(-5),
                TicketCount = 0,
                IdentityDisplayName = "John Smith (Consultant)",
                IdentityAccountId = "acc-n1",
                Email = "jsmith@demo-mode.org",
                BaseUrl = "https://northbeam.atlassian.net",
                Projects = ["OPS", "DATA"],
                Boards = [new BoardRef { Id = 30, Name = "Ops Kanban" }, new BoardRef { Id = 31, Name = "Data Sprint" }],
                Enabled = true
            }
        };
    }

    public static Dictionary<string, List<TicketViewModel>> GetDemoTickets()
    {
        var now = DateTimeOffset.Now;
        var config = GetDemoConfig();
        var conns = config.Connections.ToDictionary(c => c.Id);

        return new Dictionary<string, List<TicketViewModel>>
        {
            ["harborline"] = MakeHarborlineTickets(now, conns["harborline"]),
            ["quarrylabs"] = MakeQuarryLabsTickets(now, conns["quarrylabs"]),
            ["meridianpay"] = MakeMeridianPayTickets(now, conns["meridianpay"]),
            ["northbeam"] = []
        };
    }

    private static List<TicketViewModel> MakeHarborlineTickets(DateTimeOffset now, ConnectionConfig conn)
    {
        return
        [
            MakeVm(conn, "CLM-1440", "Duplicate claim numbers when two adjusters save at the same time", "To Do", "new", "Highest", 1, now.AddDays(1), now.AddMinutes(-10),
                [new BoardRef { Id = 1, Name = "Claims Sprint Board" }]),
            MakeVm(conn, "CLM-1423", "Premium recalculation times out on policies with 40+ line items", "In Development", "indeterminate", "Highest", 1, now.AddDays(1), now.AddMinutes(-25),
                [new BoardRef { Id = 1, Name = "Claims Sprint Board" }]),
            MakeVm(conn, "CLM-1377", "Export loss runs to PDF for the agency portal", "QA Verify", "indeterminate", "Medium", 3, now.AddDays(3), now.AddMinutes(-50),
                [new BoardRef { Id = 1, Name = "Claims Sprint Board" }]),
            MakeVm(conn, "CLM-1431", "Add audit trail for adjuster reassignment", "Code Review", "indeterminate", "High", 2, now.AddDays(2), now.AddHours(-2),
                [new BoardRef { Id = 1, Name = "Claims Sprint Board" }]),
            MakeVm(conn, "CLM-1398", "FNOL intake drops attachments larger than 10 MB", "Blocked – Client Input", "indeterminate", "High", 2, now.AddDays(-5), now.AddDays(-1),
                [new BoardRef { Id = 1, Name = "Claims Sprint Board" }]),
            MakeVm(conn, "PLAT-131", "Upgrade background job host to .NET 10", "In Development", "indeterminate", "Medium", 3, null, now.AddHours(-5),
                [new BoardRef { Id = 2, Name = "Platform Kanban" }]),
            // Duplicate key: PLAT-112 exists on both Harborline and Quarry Labs
            MakeVm(conn, "PLAT-112", "Rotate SQL Server service account credentials in staging", "To Do", "new", "Medium", 3, now.AddDays(9), now.AddDays(-3),
                [new BoardRef { Id = 2, Name = "Platform Kanban" }]),
        ];
    }

    private static List<TicketViewModel> MakeQuarryLabsTickets(DateTimeOffset now, ConnectionConfig conn)
    {
        return
        [
            MakeVm(conn, "RPT-58", "Subreport page breaks render a blank page in RDLC export", "In Progress", "indeterminate", "High", 2, now.AddDays(1), now.AddMinutes(-35),
                [new BoardRef { Id = 11, Name = "Reporting Board" }]),
            // Duplicate key: PLAT-112 on a different site — separate row
            MakeVm(conn, "PLAT-112", "Flaky integration test: OrderSync retries twice on HTTP 503", "In Progress", "indeterminate", "High", 2, now.AddDays(1), now.AddHours(-1),
                // On TWO boards (ticket on two boards scenario)
                [new BoardRef { Id = 10, Name = "Platform Sprint" }, new BoardRef { Id = 11, Name = "Reporting Board" }]),
            MakeVm(conn, "PLAT-140", "Missing index on Shipments.CarrierId causes table scan", "In Review", "indeterminate", "Medium", 3, null, now.AddHours(-3),
                [new BoardRef { Id = 10, Name = "Platform Sprint" }]),
            MakeVm(conn, "PLAT-127", "Carrier rate lookup waiting on vendor API key", "Blocked", "indeterminate", "High", 2, now.AddDays(-2), now.AddDays(-1),
                [new BoardRef { Id = 10, Name = "Platform Sprint" }]),
            MakeVm(conn, "RPT-61", "Validate date-range parameters on scheduled reports", "Selected for Development", "new", "Medium", 3, now.AddDays(7), now.AddDays(-2),
                [new BoardRef { Id = 11, Name = "Reporting Board" }]),
            MakeVm(conn, "PLAT-145", "Document local dev setup for Linux contributors", "Selected for Development", "new", "Low", 4, null, now.AddDays(-8),
                [new BoardRef { Id = 10, Name = "Platform Sprint" }]),
        ];
    }

    private static List<TicketViewModel> MakeMeridianPayTickets(DateTimeOffset now, ConnectionConfig conn)
    {
        // These are stale — connection needs reauthorization
        return
        [
            MakeVm(conn, "MOB-904", "Tap-to-pay sheet stays open after a declined card", "Doing", "indeterminate", "Highest", 1, now.Date, now.AddHours(-16),
                [new BoardRef { Id = 20, Name = "Mobile Sprint" }], isStale: true, lastRefresh: now.AddHours(-2)),
            MakeVm(conn, "MOB-911", "Android 15 back gesture exits the payment flow", "QA", "indeterminate", "High", 2, now.AddDays(2), now.AddHours(-18),
                [new BoardRef { Id = 20, Name = "Mobile Sprint" }], isStale: true, lastRefresh: now.AddHours(-2)),
            MakeVm(conn, "API-233", "Webhook signature check fails on retried events", "On Hold", "indeterminate", "High", 2, now.AddDays(6), now.AddDays(-2),
                [new BoardRef { Id = 21, Name = "API Kanban" }], isStale: true, lastRefresh: now.AddHours(-2)),
            MakeVm(conn, "API-240", "Rate-limit headers missing on /v2/payouts", "Backlog", "new", "Low", 4, null, now.AddDays(-5),
                [new BoardRef { Id = 21, Name = "API Kanban" }], isStale: true, lastRefresh: now.AddHours(-2)),
        ];
    }

    private static TicketViewModel MakeVm(
        ConnectionConfig conn, string key, string summary,
        string status, string statusCatKey, string priority, int priorityOrder,
        DateTimeOffset? due, DateTimeOffset updated,
        List<BoardRef> boards,
        bool isStale = false, DateTimeOffset? lastRefresh = null)
    {
        var normalizedCat = StatusNormalizer.Normalize(status, statusCatKey, conn.StatusMap);
        return new TicketViewModel
        {
            Key = key,
            Summary = summary,
            Status = status,
            StatusCategoryKey = statusCatKey,
            NormalizedCategory = normalizedCat,
            Priority = priority,
            PriorityOrder = priorityOrder,
            DueDate = due,
            Updated = updated,
            Created = updated.AddDays(-7),
            Reporter = "Alex Johnson",
            Assignee = "John Smith",
            AssigneeAccountId = $"acc-{conn.Id[..1]}1",
            ProjectKey = key.Split('-')[0],
            ProjectName = GetProjectName(key.Split('-')[0], conn.Id),
            DescriptionHtml = $"<p>This is a demo description for <strong>{key}</strong>.</p><p>It demonstrates how ADF content is rendered in the detail drawer.</p>",
            Boards = boards,
            SiteId = conn.Id,
            SiteDisplayName = conn.DisplayName,
            SiteColor = conn.Color,
            SiteBaseUrl = conn.BaseUrl,
            SiteEmail = conn.Email,
            JiraUrl = $"{conn.BaseUrl}/browse/{key}",
            IsStale = isStale,
            LastSuccessfulRefresh = lastRefresh ?? DateTimeOffset.Now
        };
    }

    private static string GetProjectName(string projectKey, string siteId) => (projectKey, siteId) switch
    {
        ("CLM", _) => "Claims Platform",
        ("PLAT", "harborline") => "Platform Services",
        ("PLAT", "quarrylabs") => "Platform",
        ("RPT", _) => "Reporting",
        ("MOB", _) => "Mobile App",
        ("API", _) => "Partner API",
        ("OPS", _) => "Operations Portal",
        ("DATA", _) => "Data Pipelines",
        _ => projectKey
    };
}
