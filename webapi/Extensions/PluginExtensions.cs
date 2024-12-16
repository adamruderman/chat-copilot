// Copyright (c) Microsoft. All rights reserved.

using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Catalyst;
using HtmlAgilityPack;
using Markdig;
using Mosaik.Core;

namespace CopilotChat.WebApi.Extensions;

internal static class PluginExtensions
{
    internal static string RemoveExtraSpaces(this string text)
    {
        text = text.Trim();
        text = Regex.Replace(text, @"\s+", " ");

        return text;
    }

    internal static HtmlDocument ToHtmlDocument(this string text)
    {
        var document = new HtmlDocument();
        document.LoadHtml(text);
        document.DocumentNode.RemoveComments();
        document.DocumentNode.RemoveScripts();
        return document;
    }

    internal static string ToHtmlFromMarkdown(this string markdownText)
    {
        return Markdown.ToHtml(markdownText);

    }

    internal static string ToWebsiteHTMLContent(this HtmlDocument document)
    {


        return document.DocumentNode.InnerHtml.Trim().RemoveExtraSpaces().ToHtmlDecodeString();

    }

    internal static string ToWebsiteTextContent(this HtmlDocument document)
    {


        return document.DocumentNode.InnerText.Trim().RemoveExtraSpaces().ToHtmlDecodeString();

    }


    internal static string RemoveStopWords(this string source)
    {
        Catalyst.Models.English.Register();
        Pipeline nlp = Pipeline.ForAsync(Language.English).Result;
        var doc = new Document(source, Language.English);
        nlp.ProcessSingle(doc);
        StringBuilder stringBuilder = new();
        foreach (var item in doc)
        {
            foreach (var line in item.Tokens.Where(i => i.POS != PartOfSpeech.DET
                                                        && i.POS != PartOfSpeech.INTJ
                                                        && i.POS != PartOfSpeech.SCONJ
                                                        && i.POS != PartOfSpeech.AUX
                                                        && i.POS != PartOfSpeech.PUNCT))
            {

                stringBuilder.AppendFormat($"{line.Value} ");

            }
        }

        return stringBuilder.ToString();
    }
    private static string ToHtmlDecodeString(this string text)
    {
        return HttpUtility.HtmlDecode(text);
    }

    private static void RemoveScripts(this HtmlNode node)
    {
        node.SelectNodes("//script")?.ToList()?.ForEach(n => n.Remove());
    }

    private static void RemoveComments(this HtmlNode node)
    {
        foreach (var n in node.ChildNodes.ToArray())
        {
            n.RemoveComments();
        }

        if (node.NodeType == HtmlNodeType.Comment)
        {
            node.Remove();
        }
    }
}
