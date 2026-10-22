using System.Windows;
using System.Windows.Controls;

namespace PerpusKu.Controls;

/// <summary>Pill status. Status menentukan warna; Text (opsional) menggantikan teks yang tampil.</summary>
public partial class StatusPill : UserControl
{
    public StatusPill() => InitializeComponent();

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(string), typeof(StatusPill), new PropertyMetadata("", OnStatusChanged));
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(StatusPill));

    public string Status { get => (string)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var pill = (StatusPill)d;
        // SetCurrentValue agar binding pada Text (bila ada) tidak terhapus
        if (string.IsNullOrEmpty(pill.Text) || pill.Text == e.OldValue as string)
            pill.SetCurrentValue(TextProperty, e.NewValue as string);
    }
}
