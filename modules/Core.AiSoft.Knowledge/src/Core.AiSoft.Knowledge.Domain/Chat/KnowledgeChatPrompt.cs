using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Core.AiSoft.Knowledge.Retrieval;
using Microsoft.Extensions.AI;

namespace Core.AiSoft.Knowledge.Chat;

/// <summary>
/// Grounded prompt: numbered sources next to the question, answers cite them as [n].
/// Stateless: no conversation history is sent (see README "Known limitations").
/// </summary>
public static partial class KnowledgeChatPrompt
{
    public const string SystemPrompt =
        """
        Bạn là trợ lý tra cứu kho tri thức nội bộ của công ty.
        Quy tắc:
        - Chỉ trả lời dựa trên các NGUỒN trong tin nhắn của người dùng. Không dùng kiến thức bên ngoài.
        - Sau mỗi ý lấy từ nguồn, ghi số nguồn trong ngoặc vuông, ví dụ [1] hoặc [1][3].
        - Nếu các nguồn không có thông tin để trả lời, nói rõ là không tìm thấy trong kho tri thức; không suy đoán.
        - Trả lời ngắn gọn, rõ ràng, bằng ngôn ngữ của câu hỏi. Có thể dùng Markdown (danh sách, in đậm).
        """;

    public static List<ChatMessage> BuildMessages(string question, IReadOnlyList<KnowledgeSource> sources)
    {
        var prompt = new StringBuilder("NGUỒN:\n");
        for (var i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            var location = source.HeadingPath.Count == 0 ? source.Title : $"{source.Title} › {string.Join(" › ", source.HeadingPath)}";
            prompt.Append($"[{i + 1}] {location}\n{source.Text}\n\n");
        }

        prompt.Append("CÂU HỎI: ").Append(question);

        return [new ChatMessage(ChatRole.System, SystemPrompt), new ChatMessage(ChatRole.User, prompt.ToString())];
    }

    /// <summary>
    /// Distinct source numbers cited in the answer, in order of first use; numbers outside 1..sourceCount are ignored.
    /// </summary>
    public static IReadOnlyList<int> GetCitedNumbers(string answer, int sourceCount)
    {
        return CitationRegex().Matches(answer)
            .Select(m => int.Parse(m.Groups["n"].Value))
            .Where(n => n >= 1 && n <= sourceCount)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Removes a leaked reasoning block and surrounding whitespace.
    /// </summary>
    public static string CleanAnswer(string answer) => ThinkRegex().Replace(answer, "").Trim();

    [GeneratedRegex(@"\[(?<n>\d{1,2})\]")]
    private static partial Regex CitationRegex();

    [GeneratedRegex(@"<think>.*?</think>", RegexOptions.Singleline)]
    private static partial Regex ThinkRegex();
}
