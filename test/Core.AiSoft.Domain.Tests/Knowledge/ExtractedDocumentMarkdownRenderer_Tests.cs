using Core.AiSoft.Knowledge.Extraction;
using Shouldly;
using Xunit;

namespace Core.AiSoft.Knowledge;

public class ExtractedDocumentMarkdownRenderer_Tests
{
    [Fact]
    public void Writes_Only_The_Heading_Levels_That_Change()
    {
        var document = new ExtractedDocument([
            new ExtractedSection([], "Lời mở đầu."),
            new ExtractedSection(["Chương 1", "Điều 1"], "Nội dung điều 1."),
            new ExtractedSection(["Chương 1", "Điều 2"], "Nội dung điều 2."),
            new ExtractedSection(["Chương 2"], "Nội dung chương 2.")
        ]);

        ExtractedDocumentMarkdownRenderer.Render(document).ShouldBe(
            "Lời mở đầu.\n\n" +
            "# Chương 1\n\n## Điều 1\n\nNội dung điều 1.\n\n" +
            "## Điều 2\n\nNội dung điều 2.\n\n" +
            "# Chương 2\n\nNội dung chương 2.\n");
    }

    [Fact]
    public void Marks_Each_Pdf_Page()
    {
        var document = new ExtractedDocument([
            new ExtractedSection([], "Trang một.", PageNumber: 1),
            new ExtractedSection([], "Trang hai.", PageNumber: 2)
        ]);

        ExtractedDocumentMarkdownRenderer.Render(document).ShouldBe(
            "<!-- page:1 -->\n\nTrang một.\n\n<!-- page:2 -->\n\nTrang hai.\n");
    }

    [Fact]
    public void Caps_Deep_Headings_At_Level_Six()
    {
        var document = new ExtractedDocument([
            new ExtractedSection(["1", "2", "3", "4", "5", "6", "7"], "Sâu.")
        ]);

        ExtractedDocumentMarkdownRenderer.Render(document).ShouldContain("###### 7\n");
    }
}
