using DotNet8WebAPI.Model;
using System.Text.RegularExpressions;

namespace DotNet8WebAPI.Services.Knowledge;

public sealed class LocalMarkdownKnowledgeService : IKnowledgeService
{
    private const int MaxDocuments = 100;
    private const int MaxDocumentBytes = 256 * 1024;
    private const int MaxResults = 3;
    private const int MaxExcerptLength = 1000;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "as", "at", "be", "by", "do", "for", "from",
        "how", "i", "in", "is", "it", "me", "of", "on", "or", "the", "this", "to",
        "was", "what", "when", "where", "which", "who", "why", "with"
    };

    private readonly string _knowledgeDirectory;
    private readonly ILogger<LocalMarkdownKnowledgeService> _logger;

    public LocalMarkdownKnowledgeService(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<LocalMarkdownKnowledgeService> logger)
    {
        var configuredDirectory = configuration["Knowledge:Directory"];
        _knowledgeDirectory = Path.GetFullPath(
            string.IsNullOrWhiteSpace(configuredDirectory)
                ? Path.Combine(environment.ContentRootPath, "docs", "knowledge")
                : Path.IsPathRooted(configuredDirectory)
                    ? configuredDirectory
                    : Path.Combine(environment.ContentRootPath, configuredDirectory));
        _logger = logger;
    }

    public async Task<KnowledgeSearchResult> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new KnowledgeSearchResult
            {
                Status = "NoResults",
                Message = "Provide a question to search the application knowledge documents."
            };
        }

        if (!Directory.Exists(_knowledgeDirectory))
        {
            return new KnowledgeSearchResult
            {
                Status = "Unavailable",
                Message = "The configured application knowledge directory was not found."
            };
        }

        try
        {
            var files = Directory.EnumerateFiles(
                    _knowledgeDirectory,
                    "*.md",
                    SearchOption.AllDirectories)
                .Take(MaxDocuments)
                .ToArray();

            if (files.Length == 0)
            {
                return new KnowledgeSearchResult
                {
                    Status = "Unavailable",
                    Message = "The application knowledge directory contains no Markdown documents."
                };
            }

            var queryTerms = Tokenize(query)
                .Where(term => !StopWords.Contains(term))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (queryTerms.Length == 0)
            {
                return new KnowledgeSearchResult
                {
                    Status = "NoResults",
                    Message = "The question did not contain searchable terms."
                };
            }

            var matches = new List<(int Score, string Source, string Excerpt)>();

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileInfo = new FileInfo(file);
                if (fileInfo.Length > MaxDocumentBytes)
                {
                    _logger.LogWarning(
                        "Skipping knowledge document larger than {MaxBytes} bytes: {DocumentName}",
                        MaxDocumentBytes,
                        fileInfo.Name);
                    continue;
                }

                var content = await File.ReadAllTextAsync(file, cancellationToken);
                var relativePath = Path.GetRelativePath(_knowledgeDirectory, file)
                    .Replace(Path.DirectorySeparatorChar, '/');

                foreach (var paragraph in Regex.Split(content, @"\r?\n\s*\r?\n")
                    .Select(value => value.Trim())
                    .Where(value => value.Length > 0))
                {
                    var paragraphTerms = Tokenize(paragraph);
                    var score = queryTerms.Sum(term =>
                        paragraphTerms.Count(candidate =>
                            candidate.Equals(term, StringComparison.OrdinalIgnoreCase)));

                    if (score == 0)
                    {
                        continue;
                    }

                    var excerpt = paragraph.Length > MaxExcerptLength
                        ? paragraph[..MaxExcerptLength]
                        : paragraph;
                    matches.Add((score, relativePath, excerpt));
                }
            }

            var citations = matches
                .OrderByDescending(match => match.Score)
                .ThenBy(match => match.Source, StringComparer.OrdinalIgnoreCase)
                .Take(MaxResults)
                .Select(match => new KnowledgeCitation
                {
                    Source = match.Source,
                    Excerpt = match.Excerpt
                })
                .ToArray();

            return new KnowledgeSearchResult
            {
                Status = citations.Length > 0 ? "Found" : "NoResults",
                Citations = citations,
                Message = citations.Length == 0
                    ? "No matching application knowledge documents were found."
                    : null
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search application knowledge documents.");
            return new KnowledgeSearchResult
            {
                Status = "Unavailable",
                Message = "Application knowledge could not be searched."
            };
        }
    }

    private static string[] Tokenize(string text)
    {
        return Regex.Matches(text.ToLowerInvariant(), @"[a-z0-9_]{2,}")
            .Select(match => match.Value)
            .ToArray();
    }
}
