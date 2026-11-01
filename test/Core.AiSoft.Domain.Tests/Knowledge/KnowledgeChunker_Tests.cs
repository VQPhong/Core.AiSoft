using System.Linq;
using Core.AiSoft.Knowledge.Extraction;
using Core.AiSoft.Knowledge.PostIndexing;
using Shouldly;
using Xunit;

namespace Core.AiSoft.Knowledge;

public class KnowledgeChunker_Tests
{
    private static readonly KnowledgeChunkingOptions Options = new() { MaxLength = 200, OverlapLength = 40 };

    [Fact]
    public void Short_Sections_Become_One_Chunk_Each_And_Empty_Ones_Are_Dropped()
    {
        var document = new ExtractedDocument([
            new ExtractedSection([], "  Intro  "),
            new ExtractedSection(["A"], "   "),
            new ExtractedSection(["A", "B"], "Body", PageNumber: 2)
        ]);

        var chunks = KnowledgeChunker.Split(document, Options);

        chunks.Select(c => c.Text).ShouldBe(["Intro", "Body"]);
        chunks.Select(c => c.Index).ShouldBe([0, 1]);
        chunks[1].HeadingPath.ShouldBe(["A", "B"]);
        chunks[1].PageNumber.ShouldBe(2);
    }

    [Fact]
    public void Long_Section_Is_Cut_At_Sentence_Ends_Within_The_Limit()
    {
        var sentences = Enumerable.Range(1, 12).Select(i => $"Câu số {i} nói về quy trình nghỉ phép của nhân viên.").ToList();
        var document = new ExtractedDocument([new ExtractedSection(["Nghỉ phép"], string.Join(" ", sentences))]);

        var chunks = KnowledgeChunker.Split(document, Options);

        chunks.Count.ShouldBeGreaterThan(1);
        chunks.ShouldAllBe(c => c.Text.Length <= Options.MaxLength);
        chunks.ShouldAllBe(c => c.Text.EndsWith('.'));
        chunks.ShouldAllBe(c => c.HeadingPath.Single() == "Nghỉ phép");

        // Every sentence survives.
        sentences.ShouldAllBe(s => chunks.Any(c => c.Text.Contains(s)));
    }

    [Fact]
    public void Next_Chunk_Starts_With_The_Last_Whole_Sentences_Of_The_Previous_One()
    {
        var text = string.Join(" ", Enumerable.Range(1, 12).Select(i => $"Sentence {i} is here."));
        var chunks = KnowledgeChunker.Split(new ExtractedDocument([new ExtractedSection([], text)]), Options);

        chunks.Count.ShouldBeGreaterThan(1);
        for (var i = 1; i < chunks.Count; i++)
        {
            var overlap = KnowledgeChunker.Tail(chunks[i - 1].Text, Options.OverlapLength);

            overlap.ShouldStartWith("Sentence");
            overlap.ShouldEndWith("is here.");
            chunks[i - 1].Text.ShouldEndWith(overlap);
            chunks[i].Text.ShouldStartWith(overlap);
        }
    }

    [Fact]
    public void Overlap_Never_Crosses_A_Heading()
    {
        var longText = string.Join(" ", Enumerable.Range(1, 12).Select(i => $"Sentence {i} is here."));
        var chunks = KnowledgeChunker.Split(new ExtractedDocument([
            new ExtractedSection(["A"], longText),
            new ExtractedSection(["B"], "Second section.")
        ]), Options);

        chunks.Last().Text.ShouldBe("Second section.");
    }

    [Fact]
    public void Sentence_Without_Full_Stops_Falls_Back_To_Commas_Then_Words()
    {
        var clauses = string.Join(", ", Enumerable.Range(1, 30).Select(i => $"mục {i}"));
        var pieces = KnowledgeChunker.SplitText(clauses, 60);

        pieces.ShouldAllBe(p => p.Length <= 60);
        pieces.Take(pieces.Count - 1).ShouldAllBe(p => p.EndsWith(','));

        var words = string.Join(" ", Enumerable.Repeat("word", 40));
        KnowledgeChunker.SplitText(words, 30).ShouldAllBe(p => p.Length <= 30 && !p.Contains("wor d"));
    }

    [Fact]
    public void Decimal_Numbers_Are_Not_Sentence_Ends()
    {
        var pieces = KnowledgeChunker.SplitText("Phiên bản 3.5 có 1.200 người dùng. Bản 4.0 thì sao.", 40);

        pieces.ShouldBe(["Phiên bản 3.5 có 1.200 người dùng.", "Bản 4.0 thì sao."]);
    }

    [Fact]
    public void Fenced_Code_Block_With_Blank_Lines_Stays_Whole()
    {
        var text = "Intro paragraph.\n\n```\nline 1\n\nline 2\n```\n\nOutro paragraph.";

        var pieces = KnowledgeChunker.SplitText(text, 30);

        pieces.ShouldContain("```\nline 1\n\nline 2\n```");
    }

    [Fact]
    public void Text_Without_Any_Boundary_Is_Hard_Cut()
    {
        var pieces = KnowledgeChunker.SplitText(new string('x', 75), 30);

        pieces.Select(p => p.Length).ShouldBe([30, 30, 15]);
    }

    [Fact]
    public void Tail_Prefers_Sentence_Then_Clause_Then_Word_Boundaries()
    {
        KnowledgeChunker.Tail("First sentence here. Second one.", 15).ShouldBe("Second one.");
        KnowledgeChunker.Tail("alpha beta gamma, delta epsilon", 16).ShouldBe("delta epsilon");
        KnowledgeChunker.Tail("alpha beta gamma delta", 12).ShouldBe("gamma delta");
        KnowledgeChunker.Tail("short", 40).ShouldBe("short");
        KnowledgeChunker.Tail(new string('x', 50), 10).ShouldBe("");
    }
}
