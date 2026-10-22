using System.Text.RegularExpressions;
using Microsoft.Playwright;
using PolsanLeave.TestInfrastructure;
using static Microsoft.Playwright.Assertions;

namespace PolsanLeave.E2E;

/// <summary>
/// Spec 0001 UI flows in a real browser. "Today" is Thursday 2026-10-22 (fixed clock). Each test uses its
/// own fictional employee from seed/dev-seed.json so tests never share data.
/// </summary>
[Trait("Category", "E2E")]
public sealed class LeaveRequestFlowTests(E2EApp app) : IClassFixture<E2EApp>
{
    // Additional dev-seed employees with the Employee role.
    private static readonly TestUser Burak = TestUser.Employee("oid-burak", "burak.koc@ornek.polsan.test", "Burak Koç"); // 1001: 18 + 20 = 38
    private static readonly TestUser Can = TestUser.Employee("oid-can", "can.ozturk@ornek.polsan.test", "Can Öztürk"); // 1003: 0 + 14 = 14
    private static readonly TestUser Elif = TestUser.Employee("oid-elif", "elif.sahin@ornek.polsan.test", "Elif Şahin"); // 1004: hired 2026-05-04 → 0

    [Fact]
    public async Task NoEmployeeRole_SeesAccessDeniedPage()
    {
        var page = await app.OpenAsAsync(PolsanLeaveAppFactory.WithoutRole);

        await Expect(page.GetByTestId("access-denied")).ToHaveTextAsync("Bu uygulamayı kullanma yetkiniz yok.");
    }

    [Fact]
    public async Task NoProfile_HomeShowsProfileMissing_NoRequestButton()
    {
        var page = await app.OpenAsAsync(PolsanLeaveAppFactory.Stranger);

        await Expect(page.GetByTestId("profile-missing")).ToHaveTextAsync("Profiliniz tanımlı değil, İK ile iletişime geçin.");
        await Expect(page.GetByTestId("new-request")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task NoRequests_ShowsEmptyMessage()
    {
        var page = await app.OpenAsAsync(Elif);

        await Expect(page.GetByTestId("requests-empty")).ToHaveTextAsync("Henüz izin talebiniz yok.");
        await Expect(page.GetByTestId("balance-available")).ToHaveTextAsync("0 gün");
    }

    [Fact]
    public async Task Preview_ShowsDaysAndAvailableAfter()
    {
        // Hasan Yıldız: 22 opening + 20 (14th anniversary 2026-01-16, age 50) = 42.
        var page = await app.OpenFormAsAsync(PolsanLeaveAppFactory.Hasan);

        await page.GetByLabel("Başlangıç tarihi").FillAsync("2026-11-02");
        await page.GetByLabel("Bitiş tarihi").FillAsync("2026-11-06");

        await Expect(page.GetByTestId("preview")).ToHaveTextAsync("Bu talep 5 iş günü tutar. Talep sonrası kullanılabilir bakiyeniz: 37 gün.");
    }

    [Fact]
    public async Task Preview_InvalidRange_ShowsValidationMessage()
    {
        var page = await app.OpenFormAsAsync(PolsanLeaveAppFactory.Hasan);

        await page.GetByLabel("Başlangıç tarihi").FillAsync("2026-11-06");
        await page.GetByLabel("Bitiş tarihi").FillAsync("2026-11-02");

        await Expect(page.GetByTestId("preview")).ToHaveTextAsync("Bitiş tarihi başlangıç tarihinden önce olamaz.");
    }

    [Fact]
    public async Task SubmitValidRequest_ShowsSuccess_AppearsOnTopAsBeklemede()
    {
        var page = await app.OpenFormAsAsync(PolsanLeaveAppFactory.Ayse);

        await page.GetByLabel("Başlangıç tarihi").FillAsync("2026-11-02");
        await page.GetByLabel("Bitiş tarihi").FillAsync("2026-11-04");
        await page.GetByTestId("submit").ClickAsync();

        await Expect(page.GetByTestId("created")).ToHaveTextAsync("İzin talebiniz oluşturuldu ve yöneticinizin onayına gönderildi.");
        var first = page.GetByTestId("request-row").First;
        await Expect(first).ToContainTextAsync("02.11.2026");
        await Expect(first.GetByTestId("request-status")).ToHaveTextAsync("Beklemede");
        await Expect(page.GetByTestId("balance-pending")).ToHaveTextAsync("3 gün");
    }

    [Fact]
    public async Task InvalidSubmit_KeepsValues_ShowsFieldError()
    {
        var page = await app.OpenFormAsAsync(Elif);

        await page.GetByLabel("Başlangıç tarihi").FillAsync("2026-11-02");
        await page.GetByLabel("Bitiş tarihi").FillAsync("2026-11-02");
        await page.GetByLabel("Açıklama (isteğe bağlı, en fazla 500 karakter)").FillAsync("Doktor randevusu");
        await page.GetByTestId("submit").ClickAsync();

        await Expect(page.Locator("#endDate-error")).ToHaveTextAsync("Talep edilen gün sayısı kullanılabilir bakiyenizi aşıyor.");
        await Expect(page.GetByLabel("Bitiş tarihi")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.GetByLabel("Başlangıç tarihi")).ToHaveValueAsync("2026-11-02");
        await Expect(page.GetByLabel("Açıklama (isteğe bağlı, en fazla 500 karakter)")).ToHaveValueAsync("Doktor randevusu");
        await Expect(page).ToHaveURLAsync(new Regex("/izin/yeni$"));
    }

    [Fact]
    public async Task DoubleClickSubmit_CreatesSingleRequest()
    {
        var page = await app.OpenFormAsAsync(Burak);

        await page.GetByLabel("Başlangıç tarihi").FillAsync("2026-11-09");
        await page.GetByLabel("Bitiş tarihi").FillAsync("2026-11-10");
        await page.GetByTestId("submit").DblClickAsync();

        await Expect(page.GetByTestId("created")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("request-row")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task NoManager_FormShowsNoApprover_SubmitDisabled()
    {
        var page = await app.OpenFormAsAsync(PolsanLeaveAppFactory.Selin);

        await Expect(page.GetByTestId("no-approver")).ToHaveTextAsync("Onaylayıcı tanımlı değil, İK ile iletişime geçin.");
        await Expect(page.GetByTestId("submit")).ToBeDisabledAsync();
    }

    [Fact]
    public async Task MyRequests_ShowsColumnsTurkishStatus_NewestFirst()
    {
        var page = await app.OpenFormAsAsync(Can);
        await SubmitAsync(page, "2026-11-16", "2026-11-17");
        app.Factory.Time.Advance(TimeSpan.FromMinutes(5));
        page = await app.OpenFormAsAsync(Can);
        await SubmitAsync(page, "2026-11-02", "2026-11-02");

        await Expect(page.Locator("[data-testid=requests] thead th")).ToHaveTextAsync(["Başlangıç", "Bitiş", "İş günü", "Durum", "Oluşturulma"]);
        var rows = page.GetByTestId("request-row");
        await Expect(rows).ToHaveCountAsync(2);
        await Expect(rows.Nth(0)).ToContainTextAsync("02.11.2026");
        await Expect(rows.Nth(1)).ToContainTextAsync("16.11.2026");
        await Expect(rows.Nth(0).GetByTestId("request-status")).ToHaveTextAsync("Beklemede");
        await Expect(rows.Nth(0)).ToContainTextAsync("22.10.2026 09:05");
    }

    [Fact]
    public async Task NewRequestForm_KeyboardOnly_LabelsAndErrorAssociation()
    {
        var page = await app.OpenFormAsAsync(Elif);

        var startDate = page.GetByLabel("Başlangıç tarihi");
        var endDate = page.GetByLabel("Bitiş tarihi");
        await startDate.FocusAsync();
        await page.Keyboard.TypeAsync("02112026");
        await TabUntilFocusedAsync(page, endDate);
        await page.Keyboard.TypeAsync("02112026");
        await TabUntilFocusedAsync(page, page.GetByLabel("Açıklama (isteğe bağlı, en fazla 500 karakter)"));
        await TabUntilFocusedAsync(page, page.GetByTestId("submit"));
        await Expect(startDate).ToHaveValueAsync("2026-11-02");
        await Expect(endDate).ToHaveValueAsync("2026-11-02");
        await page.Keyboard.PressAsync("Enter");

        await Expect(endDate).ToHaveAttributeAsync("aria-describedby", "endDate-error");
        await Expect(page.Locator("#endDate-error")).ToHaveAttributeAsync("role", "alert");
        await Expect(page.Locator("#endDate-error")).ToHaveTextAsync("Talep edilen gün sayısı kullanılabilir bakiyenizi aşıyor.");
    }

    /// <summary>
    /// Tab forward until <paramref name="target"/> has focus. Chromium's date input also tabs through its own
    /// day/month/year segments, so the next field is a few presses away; a field it never reaches fails.
    /// </summary>
    private static async Task TabUntilFocusedAsync(IPage page, ILocator target, int maxPresses = 4)
    {
        for (var i = 0; i < maxPresses; i++)
        {
            await page.Keyboard.PressAsync("Tab");
            if (await target.EvaluateAsync<bool>("el => el === document.activeElement"))
            {
                return;
            }
        }

        await Expect(target).ToBeFocusedAsync();
    }

    private static async Task SubmitAsync(IPage page, string start, string end)
    {
        await page.GetByLabel("Başlangıç tarihi").FillAsync(start);
        await page.GetByLabel("Bitiş tarihi").FillAsync(end);
        await page.GetByTestId("submit").ClickAsync();
        await Expect(page.GetByTestId("created")).ToBeVisibleAsync();
    }
}
