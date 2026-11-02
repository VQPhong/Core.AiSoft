using System;
using System.Linq;
using Core.AiSoft.Knowledge.Chat;
using Core.AiSoft.Knowledge.Retrieval;
using Microsoft.Extensions.AI;
using Shouldly;
using Xunit;

namespace Core.AiSoft.Knowledge;

public class KnowledgeChatPrompt_Tests
{
    [Fact]
    public void Only_System_Prompt_And_Question_With_Numbered_Sources_Are_Sent()
    {
        var sources = new[]
        {
            Source("Nghỉ phép", ["Đăng ký"], "Gửi đơn trước 3 ngày."),
            Source("Lương", [], "Trả lương ngày 5.")
        };

        var messages = KnowledgeChatPrompt.BuildMessages("Đăng ký nghỉ phép thế nào?", sources);

        messages.Select(m => m.Role).ShouldBe([ChatRole.System, ChatRole.User]);
        messages[1].Text.ShouldContain("[1] Nghỉ phép › Đăng ký\nGửi đơn trước 3 ngày.");
        messages[1].Text.ShouldContain("[2] Lương\nTrả lương ngày 5.");
        messages[1].Text.ShouldEndWith("CÂU HỎI: Đăng ký nghỉ phép thế nào?");
    }

    [Fact]
    public void Cited_Numbers_Are_Distinct_In_Order_And_Within_Range()
    {
        KnowledgeChatPrompt.GetCitedNumbers("A [2], B [1][2], C [7], D [0].", sourceCount: 3).ShouldBe([2, 1]);
        KnowledgeChatPrompt.GetCitedNumbers("No citation.", sourceCount: 3).ShouldBeEmpty();
    }

    [Fact]
    public void Leaked_Reasoning_Is_Removed()
    {
        KnowledgeChatPrompt.CleanAnswer("<think>\nlet me see\n</think>\n\nHà Nội [1].").ShouldBe("Hà Nội [1].");
    }

    private static KnowledgeSource Source(string title, string[] headingPath, string text)
        => new(Guid.NewGuid(), title, "slug", 0, headingPath, text, 0.8f);
}
