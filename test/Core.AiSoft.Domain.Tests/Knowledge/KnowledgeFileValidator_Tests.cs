using System.IO;
using System.IO.Compression;
using System.Text;
using Core.AiSoft.Knowledge;
using Core.AiSoft.Knowledge.Documents;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace Core.AiSoft.Knowledge;

/// <summary>
/// Pure unit tests: the validator has no infrastructure dependencies, so it is created directly.
/// </summary>
public class KnowledgeFileValidator_Tests
{
    private readonly KnowledgeFileValidator _validator = CreateValidator(maxFileSize: 1024);

    [Fact]
    public void Should_Accept_Pdf()
    {
        var (extension, contentType) = _validator.Validate("report.PDF", Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj"));

        extension.ShouldBe(".pdf");
        contentType.ShouldBe("application/pdf");
    }

    [Fact]
    public void Should_Accept_Docx()
    {
        var (extension, _) = _validator.Validate("notes.docx", CreateZip("word/document.xml"));

        extension.ShouldBe(".docx");
    }

    [Theory]
    [InlineData("readme.md")]
    [InlineData("notes.txt")]
    public void Should_Accept_Utf8_Text(string fileName)
    {
        _validator.Validate(fileName, Encoding.UTF8.GetBytes("# Tiêu đề\nNội dung tiếng Việt"));
    }

    [Theory]
    [InlineData("setup.exe")]
    [InlineData("archive.zip")]
    [InlineData("noextension")]
    public void Should_Reject_Not_Allowed_Extension(string fileName)
    {
        ShouldFailWith(KnowledgeErrorCodes.FileTypeNotAllowed, () => _validator.Validate(fileName, [1, 2, 3]));
    }

    [Fact]
    public void Should_Reject_Executable_Renamed_To_Pdf()
    {
        ShouldFailWith(KnowledgeErrorCodes.FileContentMismatch,
            () => _validator.Validate("invoice.pdf", [0x4D, 0x5A, 0x90, 0x00, 0x03]));
    }

    [Fact]
    public void Should_Reject_Plain_Zip_Renamed_To_Docx()
    {
        ShouldFailWith(KnowledgeErrorCodes.FileContentMismatch,
            () => _validator.Validate("notes.docx", CreateZip("data/readme.txt")));
    }

    [Fact]
    public void Should_Reject_Text_With_Nul_Byte()
    {
        ShouldFailWith(KnowledgeErrorCodes.FileContentMismatch,
            () => _validator.Validate("notes.txt", [0x68, 0x69, 0x00, 0x21]));
    }

    [Fact]
    public void Should_Reject_Text_That_Is_Not_Utf8()
    {
        ShouldFailWith(KnowledgeErrorCodes.FileContentMismatch,
            () => _validator.Validate("notes.md", [0x68, 0xC3, 0x28]));
    }

    [Fact]
    public void Should_Reject_Empty_File()
    {
        ShouldFailWith(KnowledgeErrorCodes.EmptyFile, () => _validator.Validate("notes.txt", []));
    }

    [Fact]
    public void Should_Reject_File_Over_Limit()
    {
        ShouldFailWith(KnowledgeErrorCodes.FileTooLarge, () => _validator.Validate("notes.txt", new byte[1025]));
    }

    private static KnowledgeFileValidator CreateValidator(long maxFileSize)
    {
        var options = new KnowledgeUploadOptions { MaxFileSize = maxFileSize };
        return new KnowledgeFileValidator(Options.Create(options));
    }

    private static byte[] CreateZip(string entryName)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(archive.CreateEntry(entryName).Open());
            writer.Write("<w:document/>");
        }

        return stream.ToArray();
    }

    private static void ShouldFailWith(string code, System.Action action)
    {
        Should.Throw<BusinessException>(action).Code.ShouldBe(code);
    }
}
