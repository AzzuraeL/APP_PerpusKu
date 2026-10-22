using System.Globalization;
using PerpusKu.Data.Repositories;
using PerpusKu.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PerpusKu.Services;

public class ReportService
{
    private readonly DashboardRepository _dash = new();
    private readonly PeminjamanRepository _pinjam = new();
    private static readonly CultureInfo Id = new("id-ID");
    private const string Teal = "#1A237E";
    private const string Soft = "#F5F2E9";

    public async Task ExportPdfAsync(string path, DateTime dari, DateTime sampai)
    {
        SessionService.Require(Permission.Laporan);
        var ringkasan = await _dash.GetRingkasanAsync(dari, sampai);
        var populer = await _dash.GetBukuPopulerAsync(dari, sampai, 5);
        var rows = await _pinjam.QueryAsync("", "Semua", dari, sampai);
        var oleh = SessionService.Current?.NamaLengkap ?? "-";

        await Task.Run(() =>
        {
            QuestPDF.Settings.License = LicenseType.Community;
            Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(t => t.FontSize(9.5f));

                    page.Header().PaddingBottom(10).BorderBottom(1).BorderColor(Teal).Column(col =>
                    {
                        col.Item().Text("PerpusKu").FontSize(22).Bold().FontColor(Teal);
                        col.Item().Text("Laporan Sirkulasi Perpustakaan Kampus").FontSize(12).SemiBold();
                        col.Item().Text($"Periode {dari.ToString("dd MMMM yyyy", Id)} – {sampai.ToString("dd MMMM yyyy", Id)}")
                            .FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(14).Column(col =>
                    {
                        col.Spacing(14);

                        col.Item().Row(row =>
                        {
                            row.Spacing(8);
                            Kartu(row, "Total peminjaman", ringkasan.TotalPinjam.ToString(Id));
                            Kartu(row, "Dikembalikan", ringkasan.Dikembalikan.ToString(Id));
                            Kartu(row, "Masih dipinjam", ringkasan.MasihDipinjam.ToString(Id));
                            Kartu(row, "Total denda", SirkulasiService.Rupiah(ringkasan.TotalDenda));
                        });

                        col.Item().Text("Buku paling sering dipinjam").FontSize(12).SemiBold();
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(28); c.ConstantColumn(60); c.RelativeColumn(3); c.RelativeColumn(2); c.ConstantColumn(50);
                            });
                            t.Header(h =>
                            {
                                h.Cell().Element(Head).Text("#");
                                h.Cell().Element(Head).Text("Kode");
                                h.Cell().Element(Head).Text("Judul");
                                h.Cell().Element(Head).Text("Penulis");
                                h.Cell().Element(Head).AlignRight().Text("Pinjam");
                            });
                            if (populer.Count == 0)
                                t.Cell().ColumnSpan(5).Element(Body).Text("Tidak ada data pada periode ini.");
                            foreach (var b in populer)
                            {
                                t.Cell().Element(Body).Text(b.Peringkat.ToString());
                                t.Cell().Element(Body).Text(b.KodeBuku);
                                t.Cell().Element(Body).Text(b.Judul);
                                t.Cell().Element(Body).Text(b.Penulis);
                                t.Cell().Element(Body).AlignRight().Text(b.JumlahPinjam.ToString());
                            }
                        });

                        col.Item().Text("Rincian transaksi").FontSize(12).SemiBold();
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(62); c.RelativeColumn(3); c.RelativeColumn(2); c.ConstantColumn(62); c.ConstantColumn(62); c.ConstantColumn(64); c.ConstantColumn(54);
                            });
                            t.Header(h =>
                            {
                                h.Cell().Element(Head).Text("Pinjam");
                                h.Cell().Element(Head).Text("Judul");
                                h.Cell().Element(Head).Text("Anggota");
                                h.Cell().Element(Head).Text("Tempo");
                                h.Cell().Element(Head).Text("Kembali");
                                h.Cell().Element(Head).Text("Status");
                                h.Cell().Element(Head).AlignRight().Text("Denda");
                            });
                            if (rows.Count == 0)
                                t.Cell().ColumnSpan(7).Element(Body).Text("Tidak ada transaksi pada periode ini.");
                            foreach (var p in rows)
                            {
                                t.Cell().Element(Body).Text(p.TanggalPinjam.ToString("dd/MM/yy"));
                                t.Cell().Element(Body).Text(p.Judul);
                                t.Cell().Element(Body).Text(p.NamaAnggota);
                                t.Cell().Element(Body).Text(p.JatuhTempo.ToString("dd/MM/yy"));
                                t.Cell().Element(Body).Text(p.TanggalKembali?.ToString("dd/MM/yy") ?? "-");
                                t.Cell().Element(Body).Text(p.Status == "Jatuh tempo hari ini" ? "Tempo" : p.Status);
                                t.Cell().Element(Body).AlignRight().Text(p.DendaTampil > 0 ? p.DendaTampil.ToString("N0", Id) : "-");
                            }
                        });
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text($"Dicetak {DateTime.Now.ToString("dd MMM yyyy HH:mm", Id)} oleh {oleh}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                        row.AutoItem().Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                            t.Span("Halaman ");
                            t.CurrentPageNumber();
                            t.Span(" / ");
                            t.TotalPages();
                        });
                    });
                });
            }).GeneratePdf(path);
        });
    }

    private static void Kartu(RowDescriptor row, string label, string value)
    {
        row.RelativeItem().Background(Soft).Border(1).BorderColor("#99F6E4").Padding(8).Column(c =>
        {
            c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken2);
            c.Item().Text(value).FontSize(15).Bold().FontColor(Teal);
        });
    }

    private static IContainer Head(IContainer c)
        => c.Background(Teal).PaddingVertical(5).PaddingHorizontal(4).DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold().FontSize(9));

    private static IContainer Body(IContainer c)
        => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(4);
}
