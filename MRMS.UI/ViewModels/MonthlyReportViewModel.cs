using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using SkiaSharp;

namespace MRMS.UI.ViewModels;

public partial class MonthlyReportViewModel : ObservableObject
{
    private readonly IMonthlyReportService _monthlyReportService;

    public MonthlyReportViewModel(IMonthlyReportService monthlyReportService)
    {
        _monthlyReportService = monthlyReportService;
        SelectedYear = DateTime.Today.Year;
        SelectedMonth = DateTime.Today.Month;
    }

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private int _selectedMonth;

    [ObservableProperty]
    private MonthlyReportDto? _report;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _changeText = "";

    public List<int> YearList => [.. Enumerable.Range(2024, 10)];

    public List<int> MonthList => [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

    // Chart series
    [ObservableProperty]
    private ISeries[] _netProfitSeries = [];

    [ObservableProperty]
    private ISeries[] _incomeVsExpensesSeries = [];

    [ObservableProperty]
    private ISeries[] _revenueBreakdownSeries = [];

    [ObservableProperty]
    private ISeries[] _topItemsSeries = [];

    [ObservableProperty]
    private Axis[] _xAxes = [];

    [ObservableProperty]
    private Axis[] _yAxes = [];

    private static readonly SKColor PrimaryColor = new(0x25, 0x63, 0xEB);
    private static readonly SKColor SuccessColor = new(0x10, 0xB9, 0x81);
    private static readonly SKColor DangerColor = new(0xEF, 0x44, 0x44);
    private static readonly SKColor WarningColor = new(0xF5, 0x9E, 0x0B);
    private static readonly SKColor InfoColor = new(0x06, 0xB6, 0xD4);
    private static readonly SKColor SecondaryColor = new(0x7C, 0x3A, 0xED);

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;

        Report = await _monthlyReportService.GetMonthlyReportAsync(SelectedYear, SelectedMonth);

        BuildCharts();

        IsLoading = false;
    }

    private void BuildCharts()
    {
        if (Report is null) return;

        var months = Report.Rows.Select(r => r.MonthName).ToArray();

        // 1. Net Profit Monthly (column chart)
        NetProfitSeries =
        [
            new ColumnSeries<double>
            {
                Values = Report.Rows.Select(r => (double)r.NetProfit).ToArray(),
                Name = "صافي الربح",
                Fill = new SolidColorPaint(PrimaryColor),
                Stroke = null,
                AnimationsSpeed = TimeSpan.FromMilliseconds(600)
            }
        ];

        // 2. Income vs Expenses (dual column chart)
        IncomeVsExpensesSeries =
        [
            new ColumnSeries<double>
            {
                Values = Report.Rows.Select(r => (double)r.TotalRevenue).ToArray(),
                Name = "الإيرادات",
                Fill = new SolidColorPaint(SuccessColor),
                Stroke = null,
                AnimationsSpeed = TimeSpan.FromMilliseconds(600)
            },
            new ColumnSeries<double>
            {
                Values = Report.Rows.Select(r => (double)(r.Expenses + r.CostOfGoodsSold + r.PartsCost)).ToArray(),
                Name = "المصروفات + التكاليف",
                Fill = new SolidColorPaint(DangerColor),
                Stroke = null,
                AnimationsSpeed = TimeSpan.FromMilliseconds(600)
            }
        ];

        // 3. Sales vs Repair Revenue (stacked column)
        RevenueBreakdownSeries =
        [
            new StackedColumnSeries<double>
            {
                Values = Report.Rows.Select(r => (double)r.SalesRevenue).ToArray(),
                Name = "مبيعات",
                Fill = new SolidColorPaint(PrimaryColor),
                Stroke = null
            },
            new StackedColumnSeries<double>
            {
                Values = Report.Rows.Select(r => (double)r.RepairRevenue).ToArray(),
                Name = "صيانة",
                Fill = new SolidColorPaint(SuccessColor),
                Stroke = null
            }
        ];

        // 4. Top 10 Items (horizontal bar)
        TopItemsSeries =
        [
            new RowSeries<double>
            {
                Values = Report.TopSellingItems.Select(i => (double)i.TotalRevenue).ToArray(),
                Name = "إيرادات",
                Fill = new SolidColorPaint(WarningColor),
                Stroke = null,
                AnimationsSpeed = TimeSpan.FromMilliseconds(600)
            }
        ];

        // Common X axes (categories)
        var catAxes = new Axis
        {
            Labels = months,
            LabelsRotation = -45,
            TextSize = 11,
            LabelsPaint = new SolidColorPaint(new SKColor(0x64, 0x74, 0x8B))
        };

        // Top items X axis
        var topItemsAxis = new Axis
        {
            Labels = Report.TopSellingItems.Select(i => i.ItemName.Length > 15 ? i.ItemName[..15] + ".." : i.ItemName).ToArray(),
            LabelsRotation = -30,
            TextSize = 11,
            LabelsPaint = new SolidColorPaint(new SKColor(0x64, 0x74, 0x8B))
        };

        // Y axes
        var moneyAxis = new Axis
        {
            Labeler = v => v.ToString("N0"),
            TextSize = 12,
            LabelsPaint = new SolidColorPaint(new SKColor(0x64, 0x74, 0x8B))
        };

        XAxes = [catAxes];
        YAxes = [moneyAxis];

        // Store property for top items X axis
        _topItemsXAxes = [topItemsAxis];
    }

    private Axis[]? _topItemsXAxes;

    public Axis[] TopItemsXAxes => _topItemsXAxes ?? [new Axis()];
    public Axis[] TopItemsYAxes => YAxes;
}
