using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using MRMS.Application.DTOs;
using MRMS.Core.Enums;

namespace MRMS.UI.Views;

public partial class PrintPreviewWindow : Window
{
    public PrintPreviewWindow(FixedDocument document)
    {
        InitializeComponent();
        DocViewer.Document = document;
    }

    public static FixedDocument BuildInvoice(RepairTicketDto ticket, string? warrantyInfo)
    {
        var doc = new FixedDocument();
        var pageSize = new Size(302, 1400);
        doc.DocumentPaginator.PageSize = pageSize;

        var pageContent = new PageContent();
        var fixedPage = new FixedPage { Width = pageSize.Width, Height = pageSize.Height };

        var root = new Border
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(2),
            Padding = new Thickness(14, 20, 14, 20),
            Background = Brushes.White,
            FlowDirection = FlowDirection.RightToLeft
        };
        TextElement.SetFontFamily(root, new FontFamily("Segoe UI"));

        var stack = new StackPanel();

        void AddInfoRow(StackPanel parent, string label, string value, bool isBoldValue = false)
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.Children.Add(new TextBlock(new Run(label + ":"))
            {
                FontWeight = FontWeights.SemiBold, FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            });
            var vt = new TextBlock(new Run(value))
            {
                FontSize = 10, FontWeight = isBoldValue ? FontWeights.Bold : FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(vt, 1);
            g.Children.Add(vt);
            parent.Children.Add(g);
        }

        // Header
        stack.Children.Add(new TextBlock(new Run("KMG"))
        { FontSize = 20, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
        stack.Children.Add(new TextBlock(new Run("مركز صيانة الأجهزة المحمولة"))
        { FontSize = 11, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) });
        stack.Children.Add(new Rectangle { Height = 1, Fill = Brushes.Black, Margin = new Thickness(0, 0, 0, 8) });

        // Title
        stack.Children.Add(new TextBlock(new Run("فاتورة صيانة"))
        { FontSize = 16, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });

        // Ticket Info
        var infoStack = new StackPanel();
        AddInfoRow(infoStack, "رقم الفاتورة", ticket.TicketNumber, true);
        AddInfoRow(infoStack, "تاريخ الاستلام", ticket.ReceiveDate.ToString("yyyy-MM-dd"));
        stack.Children.Add(new Border
        { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 6), Child = infoStack });

        // Customer
        var customerStack = new StackPanel();
        customerStack.Children.Add(new TextBlock(new Run("بيانات العميل"))
        { FontWeight = FontWeights.Bold, FontSize = 11, Margin = new Thickness(0, 0, 0, 4) });
        AddInfoRow(customerStack, "الاسم", ticket.CustomerName ?? "---");
        stack.Children.Add(new Border
        { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 6), Child = customerStack });

        // Device
        var deviceStack = new StackPanel();
        deviceStack.Children.Add(new TextBlock(new Run("الجهاز"))
        { FontWeight = FontWeights.Bold, FontSize = 11, Margin = new Thickness(0, 0, 0, 4) });
        AddInfoRow(deviceStack, "الجهاز", ticket.DeviceInfo ?? "---");
        stack.Children.Add(new Border
        { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 6), Child = deviceStack });

        // Repair Description
        var problemStack = new StackPanel();
        problemStack.Children.Add(new TextBlock(new Run("وصف الإصلاح"))
        { FontWeight = FontWeights.Bold, FontSize = 11, Margin = new Thickness(0, 0, 0, 4) });
        problemStack.Children.Add(new TextBlock(new Run(
            string.IsNullOrEmpty(ticket.RepairDescription) ? ticket.ProblemDescription : ticket.RepairDescription))
        { FontSize = 10, TextWrapping = TextWrapping.Wrap });
        stack.Children.Add(new Border
        { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 6), Child = problemStack });

        // Cost
        if (ticket.Cost > 0)
        {
            var costStack = new StackPanel();
            costStack.Children.Add(new TextBlock(new Run("التكلفة"))
            { FontWeight = FontWeights.Bold, FontSize = 11, Margin = new Thickness(0, 0, 0, 4) });
            AddInfoRow(costStack, "التكلفة الكلية", $"{ticket.Cost:N0} جنيه", true);
            stack.Children.Add(new Border
            { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 6), Child = costStack });
        }

        // Status & Warranty
        var statusStack = new StackPanel();
        AddInfoRow(statusStack, "الحالة", ticket.Status switch
        {
            TicketStatus.Received => "مستلم",
            TicketStatus.UnderInspection => "تحت الفحص",
            TicketStatus.WaitingForParts => "بانتظار القطع",
            TicketStatus.InRepair => "قيد الإصلاح",
            TicketStatus.Repaired => "تم الإصلاح",
            TicketStatus.Delivered => "تم التسليم",
            TicketStatus.Cancelled => "ملغي",
            _ => ticket.Status.ToString()
        });
        if (!string.IsNullOrEmpty(warrantyInfo))
            AddInfoRow(statusStack, "الضمان", warrantyInfo);
        stack.Children.Add(new Border
        { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 6), Child = statusStack });

        // Footer
        stack.Children.Add(new Rectangle { Height = 1, Fill = Brushes.Black, Margin = new Thickness(0, 0, 0, 6) });
        stack.Children.Add(new TextBlock(new Run("شكراً لتعاملكم معنا"))
        { FontSize = 12, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 2) });
        stack.Children.Add(new TextBlock(new Run("نتمنى لكم يوماً سعيداً"))
        { FontSize = 10, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center });

        root.Child = stack;
        FixedPage.SetLeft(root, 0);
        FixedPage.SetTop(root, 0);
        root.Width = pageSize.Width - 4;
        root.Height = pageSize.Height - 4;
        fixedPage.Children.Add(root);
        pageContent.Child = fixedPage;
        doc.Pages.Add(pageContent);
        return doc;
    }

    public static FixedDocument BuildReceptionReceipt(RepairTicketDto ticket)
    {
        var doc = new FixedDocument();
        var pageSize = new Size(302, 1000);
        doc.DocumentPaginator.PageSize = pageSize;

        var pageContent = new PageContent();
        var fixedPage = new FixedPage { Width = pageSize.Width, Height = pageSize.Height };

        var root = new Border
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(2),
            Padding = new Thickness(14, 20, 14, 20),
            Background = Brushes.White,
            FlowDirection = FlowDirection.RightToLeft
        };
        TextElement.SetFontFamily(root, new FontFamily("Segoe UI"));

        var stack = new StackPanel();

        void AddInfoRow(string label, string value, bool isBoldValue = false)
        {
            var g = new Grid { Margin = new Thickness(0, 0, 0, 3) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.Children.Add(new TextBlock(new Run(label + ":"))
            { FontWeight = FontWeights.SemiBold, FontSize = 10, VerticalAlignment = VerticalAlignment.Center });
            var vt = new TextBlock(new Run(value))
            { FontSize = 10, FontWeight = isBoldValue ? FontWeights.Bold : FontWeights.Normal, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(vt, 1);
            g.Children.Add(vt);
            stack.Children.Add(g);
        }

        // Header
        stack.Children.Add(new TextBlock(new Run("KMG"))
        { FontSize = 18, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
        stack.Children.Add(new TextBlock(new Run("مركز صيانة الأجهزة المحمولة"))
        { FontSize = 10, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4) });
        stack.Children.Add(new Rectangle { Height = 1, Fill = Brushes.Black, Margin = new Thickness(0, 0, 0, 8) });

        // Title
        stack.Children.Add(new TextBlock(new Run("وصل استلام جهاز"))
        { FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });

        AddInfoRow("رقم الوصل", ticket.TicketNumber, true);
        AddInfoRow("تاريخ الاستلام", ticket.ReceiveDate.ToString("yyyy-MM-dd"));
        AddInfoRow("العميل", ticket.CustomerName ?? "---");
        AddInfoRow("الجهاز", ticket.DeviceInfo ?? "---");

        stack.Children.Add(new TextBlock(new Run("المشكلة:"))
        { FontWeight = FontWeights.SemiBold, FontSize = 10, Margin = new Thickness(0, 4, 0, 2) });
        stack.Children.Add(new TextBlock(new Run(ticket.ProblemDescription))
        { FontSize = 10, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) });

        stack.Children.Add(new Rectangle { Height = 1, Fill = Brushes.Black, Margin = new Thickness(0, 4, 0, 6) });
        stack.Children.Add(new TextBlock(new Run("تم استلام الجهاز"))
        { FontSize = 11, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 2) });
        stack.Children.Add(new TextBlock(new Run("سيتم التواصل عند الانتهاء من الصيانة"))
        { FontSize = 9, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center });

        root.Child = stack;
        FixedPage.SetLeft(root, 0);
        FixedPage.SetTop(root, 0);
        root.Width = pageSize.Width - 4;
        root.Height = pageSize.Height - 4;
        fixedPage.Children.Add(root);
        pageContent.Child = fixedPage;
        doc.Pages.Add(pageContent);
        return doc;
    }
}