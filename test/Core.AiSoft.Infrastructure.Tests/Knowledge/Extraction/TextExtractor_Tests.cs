using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.Extraction;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Shouldly;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Volo.Abp;
using Xunit;

namespace Core.AiSoft.Knowledge.Extraction;

public class TextExtractor_Tests
{
    [Fact]
    public async Task PlainText_Is_One_Flat_Section_With_Normalized_Text()
    {
        // Decomposed Vietnamese ("e" + combining marks), CRLF, trailing spaces, extra blank lines, a control char.
        var raw = "Tiếng Việt  \r\n\r\n\r\n\r\nDòng\u0007 hai cuối";

        var result = await Extract(new PlainTextExtractor(), raw);

        result.HasHeadings.ShouldBeFalse();
        result.Sections.Count.ShouldBe(1);
        result.Sections[0].Text.ShouldBe("Tiếng Việt\n\nDòng hai cuối");
    }

    [Fact]
    public async Task Markdown_Splits_By_Heading_Path()
    {
        const string markdown = """
            Lời mở đầu.

            # Chương 1
            Nội dung chương 1.

            ## 1.1 Phạm vi ##
            Nội dung 1.1.

            ### 1.1.1 Chi tiết
            Nội dung 1.1.1.

            ## 1.2 Đối tượng
            Nội dung 1.2.

            # Chương 2
            Nội dung chương 2.
            """;

        var sections = (await Extract(new MarkdownTextExtractor(), markdown)).Sections;

        sections.Select(s => string.Join(" > ", s.HeadingPath)).ShouldBe(
        [
            "",
            "Chương 1",
            "Chương 1 > 1.1 Phạm vi",
            "Chương 1 > 1.1 Phạm vi > 1.1.1 Chi tiết",
            "Chương 1 > 1.2 Đối tượng",
            "Chương 2"
        ]);
        sections[3].Text.ShouldBe("Nội dung 1.1.1.");
        sections[3].Level.ShouldBe(3);
    }

    [Fact]
    public async Task Markdown_Ignores_Hashes_Inside_Code_Fences_And_Drops_Empty_Sections()
    {
        const string markdown = """
            # Hướng dẫn
            ```bash
            # đây là comment, không phải heading
            echo hi
            ```
            # Mục trống
            # Kết thúc
            Xong.
            """;

        var sections = (await Extract(new MarkdownTextExtractor(), markdown)).Sections;

        sections.Select(s => s.HeadingPath.Single()).ShouldBe(["Hướng dẫn", "Kết thúc"]);
        sections[0].Text.ShouldContain("# đây là comment");
    }

    [Fact]
    public async Task Docx_Uses_Heading_Styles_And_Flattens_Tables()
    {
        var docx = CreateDocx(body =>
        {
            body.Append(Paragraph("Mở đầu."));
            body.Append(Paragraph("Quy định chung", style: "Heading1"));
            body.Append(Paragraph("Nội dung quy định."));
            body.Append(Paragraph("Hạn mức", style: "CustomHeading"));   // based on Heading2
            body.Append(new Table(
                new TableRow(Cell("Loại"), Cell("Mức")),
                new TableRow(Cell("Tạm ứng"), Cell("50 triệu"))));
        });

        var sections = (await new DocxTextExtractor().ExtractAsync(new MemoryStream(docx))).Sections;

        sections.Select(s => string.Join(" > ", s.HeadingPath)).ShouldBe(["", "Quy định chung", "Quy định chung > Hạn mức"]);
        sections[2].Text.ShouldBe("Loại | Mức\nTạm ứng | 50 triệu");
    }

    [Fact]
    public async Task Pdf_Gives_One_Section_Per_Page()
    {
        var pdf = CreatePdf("Trang mot co du chu de vuot nguong kiem tra lop van ban.", "Trang hai cung co du so chu can thiet.");

        var sections = (await new PdfTextExtractor().ExtractAsync(new MemoryStream(pdf))).Sections;

        sections.Select(s => s.PageNumber).ShouldBe([1, 2]);
        sections[0].Text.ShouldContain("Trang mot");
        sections[1].Text.ShouldContain("Trang hai");
    }

    [Fact]
    public async Task Pdf_Without_Text_Layer_Is_Rejected()
    {
        var pdf = CreatePdf("", "");

        var error = await Should.ThrowAsync<BusinessException>(() => new PdfTextExtractor().ExtractAsync(new MemoryStream(pdf)));

        error.Code.ShouldBe(KnowledgeErrorCodes.PdfHasNoTextLayer);
    }

    [Fact]
    public async Task Corrupt_File_Is_Unreadable_Not_Retried()
    {
        var error = await Should.ThrowAsync<BusinessException>(
            () => new DocxTextExtractor().ExtractAsync(new MemoryStream("PK\u0003\u0004 not a real zip"u8.ToArray())));

        error.Code.ShouldBe(KnowledgeErrorCodes.FileUnreadable);
        error.Data["extension"].ShouldBe(".docx");
    }

    [Fact]
    public async Task Whitespace_Only_File_Has_No_Text()
    {
        var error = await Should.ThrowAsync<BusinessException>(() => Extract(new PlainTextExtractor(), " \r\n\t \n"));

        error.Code.ShouldBe(KnowledgeErrorCodes.NoTextExtracted);
    }

    [Theory]
    [InlineData(".txt", typeof(PlainTextExtractor))]
    [InlineData(".md", typeof(MarkdownTextExtractor))]
    [InlineData(".docx", typeof(DocxTextExtractor))]
    [InlineData(".pdf", typeof(PdfTextExtractor))]
    public void Each_Extension_Has_Exactly_One_Extractor(string extension, System.Type expected)
    {
        ITextExtractor[] all = [new PlainTextExtractor(), new MarkdownTextExtractor(), new DocxTextExtractor(), new PdfTextExtractor()];

        all.Where(e => e.CanExtract(extension)).Select(e => e.GetType()).ShouldBe([expected]);
    }

    private static Task<ExtractedDocument> Extract(ITextExtractor extractor, string text)
    {
        return extractor.ExtractAsync(new MemoryStream(Encoding.UTF8.GetBytes(text)));
    }

    private static Paragraph Paragraph(string text, string? style = null)
    {
        var paragraph = new Paragraph(new Run(new Text(text)));
        if (style != null)
        {
            paragraph.PrependChild(new ParagraphProperties(new ParagraphStyleId { Val = style }));
        }

        return paragraph;
    }

    private static TableCell Cell(string text)
    {
        return new TableCell(Paragraph(text));
    }

    private static byte[] CreateDocx(System.Action<Body> fill)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body());

            var styles = main.AddNewPart<StyleDefinitionsPart>();
            styles.Styles = new Styles(
                HeadingStyle("Heading1", "heading 1"),
                HeadingStyle("Heading2", "heading 2"),
                new Style(new StyleName { Val = "Custom Heading" }, new BasedOn { Val = "Heading2" })
                {
                    Type = StyleValues.Paragraph,
                    StyleId = "CustomHeading"
                });

            fill(main.Document.Body!);
            main.Document.Save();
        }

        return stream.ToArray();

        static Style HeadingStyle(string id, string name)
        {
            return new Style(new StyleName { Val = name }) { Type = StyleValues.Paragraph, StyleId = id };
        }
    }

    private static byte[] CreatePdf(params string[] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        foreach (var text in pages)
        {
            var page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
            if (text.Length > 0)
            {
                page.AddText(text, 12, new UglyToad.PdfPig.Core.PdfPoint(50, 700), font);
            }
        }

        return builder.Build();
    }
}
