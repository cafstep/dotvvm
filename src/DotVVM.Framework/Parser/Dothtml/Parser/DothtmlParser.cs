using System.Collections.Generic;
using System.Linq;
using DotVVM.Framework.Parser.Dothtml.Tokenizer;
using DotVVM.Framework.Resources;
using System.Diagnostics;
using DotVVM.Framework.Controls;
using DotVVM.Framework.Exceptions;
using System.Net;
using System;

namespace DotVVM.Framework.Parser.Dothtml.Parser
{
    /// <summary>
    /// Parses the results of the DothtmlTokenizer into a tree structure.
    /// </summary>
    public class DothtmlParser : ParserBase<DothtmlToken, DothtmlTokenType>
    {
        public static readonly HashSet<string> AutomaticClosingTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "html", "head", "body", "p", "dt", "dd", "li", "option", "thead", "th", "tbody", "tr", "td", "tfoot", "colgroup"
        };

        private Stack<DothtmlNodeWithContent> ElementHierarchy { get; set; }

        private List<DothtmlNode> CurrentElementContent
        {
            get { return ElementHierarchy.Peek().Content; }
        }

        public DothtmlRootNode Root { get; private set; }


        /// <summary>
        /// Parses the token stream and gets the node.
        /// </summary>
        public DothtmlRootNode Parse(IList<DothtmlToken> tokens)
        {
            Root = null;
            Tokens = tokens;
            CurrentIndex = 0;
            ElementHierarchy = new Stack<DothtmlNodeWithContent>();

            // read file
            var root = new DothtmlRootNode();
            root.Tokens.AddRange(Tokens);
            ElementHierarchy.Push(root);
            SkipWhitespace();

            // read directives
            while (Peek() != null && Peek().Type == DothtmlTokenType.DirectiveStart)
            {
                root.Directives.Add(ReadDirective());
            }

            SkipWhitespace();

            // read content
            while (Peek() != null)
            {
                if (Peek().Type == DothtmlTokenType.OpenTag)
                {
                    // element - check element hierarchy
                    var element = ReadElement();
                    if (ElementHierarchy.Any())
                    {
                        element.ParentElement = ElementHierarchy.Peek() as DothtmlElementNode;
                    }

                    if (!element.IsSelfClosingTag)
                    {
                        if (!element.IsClosingTag)
                        {
                            // open tag
                            CurrentElementContent.Add(element);
                            ElementHierarchy.Push(element);
                        }
                        else
                        {
                            // close tag
                            if (ElementHierarchy.Count <= 1)
                            {
                                element.NodeErrors.Add(string.Format(DothtmlParserErrors.ClosingTagHasNoMatchingOpenTag, element.FullTagName));
                                CurrentElementContent.Add(element);
                            }
                            else
                            {
                                var beginTag = (DothtmlElementNode)ElementHierarchy.Peek();
                                var beginTagName = beginTag.FullTagName;
                                if (!beginTagName.Equals(element.FullTagName, StringComparison.OrdinalIgnoreCase))
                                {
                                    element.NodeErrors.Add(string.Format(DothtmlParserErrors.ClosingTagHasNoMatchingOpenTag, beginTagName));
                                    ResolveWrongClosingTag(element);
                                    beginTag = ElementHierarchy.Peek() as DothtmlElementNode;

                                    if (beginTag != null && beginTagName != beginTag.FullTagName)
                                    {
                                        beginTag.CorrespondingEndTag = element;
                                        ElementHierarchy.Pop();
                                    }
                                    else
                                    {
                                        CurrentElementContent.Add(element);
                                    }
                                }
                                else
                                {
                                    ElementHierarchy.Pop();
                                    beginTag.CorrespondingEndTag = element;
                                }
                            }
                        }
                    }
                    else
                    {
                        // self closing tag
                        CurrentElementContent.Add(element);
                    }
                }
                else if (Peek().Type == DothtmlTokenType.OpenBinding)
                {
                    // binding
                    CurrentElementContent.Add(ReadBinding());
                }
                else if (Peek().Type == DothtmlTokenType.OpenCData)
                {
                    CurrentElementContent.Add(ReadCData());
                }
                else if (Peek().Type == DothtmlTokenType.OpenComment)
                {
                    CurrentElementContent.Add(ReadComment());
                }
                else
                {
                    // text
                    if (CurrentElementContent.Count > 0 
                        && CurrentElementContent[CurrentElementContent.Count - 1].GetType() == typeof(DothtmlLiteralNode)
                        && !((DothtmlLiteralNode)CurrentElementContent[CurrentElementContent.Count - 1]).IsComment)
                    {
                        // append to the previous literal
                        var lastLiteral = (DothtmlLiteralNode)CurrentElementContent[CurrentElementContent.Count - 1];
                        if (lastLiteral.Escape != false)
                            CurrentElementContent.Add(new DothtmlLiteralNode() { Value = Peek().Text, Tokens = { Peek() }, StartPosition = Peek().StartPosition });
                        else
                        {
                            lastLiteral.Value += Peek().Text;
                            lastLiteral.Tokens.Add(Peek());
                        }
                    }
                    else
                    {
                        CurrentElementContent.Add(new DothtmlLiteralNode() { Value = Peek().Text, Tokens = { Peek() }, StartPosition = Peek().StartPosition });
                    }
                    Read();
                }
            }

            // check element hierarchy
            if (ElementHierarchy.Count > 1)
            {
                root.NodeErrors.Add(string.Format(DothtmlParserErrors.UnexpectedEndOfInputTagNotClosed, ElementHierarchy.Peek()));
            }

            // set lengths to all nodes
            foreach (var node in root.EnumerateNodes())
            {
                node.Length = node.Tokens.Select(t => t.Length).DefaultIfEmpty(0).Sum();
            }

            Root = root;
            return root;
        }

        private void ResolveWrongClosingTag(DothtmlElementNode element)
        {
            Debug.Assert(element.IsClosingTag);
            var startElement = ElementHierarchy.Peek() as DothtmlElementNode;
            Debug.Assert(startElement != null);
            Debug.Assert(startElement.FullTagName != element.FullTagName);

            while (startElement != null && !startElement.FullTagName.Equals(element.FullTagName, StringComparison.OrdinalIgnoreCase))
            {
                ElementHierarchy.Pop();
                if (HtmlWriter.SelfClosingTags.Contains(startElement.FullTagName))
                {
                    // automatic immediate close of the tag (for <img src="">)
                    ElementHierarchy.Peek().Content.AddRange(startElement.Content);
                    startElement.Content.Clear();
                }
                else if (AutomaticClosingTags.Contains(startElement.FullTagName))
                {
                    // elements than can contain itself like <p> are closed on the first occurance of element with the same name
                    var sameElementIndex = startElement.Content.FindIndex(a => (a as DothtmlElementNode)?.FullTagName == startElement.FullTagName);
                    if (sameElementIndex >= 0)
                    {
                        var count = startElement.Content.Count - sameElementIndex;
                        ElementHierarchy.Peek().Content.AddRange(startElement.Content.Skip(sameElementIndex));
                        startElement.Content.RemoveRange(sameElementIndex, count);
                    }
                }

                // otherwise just pop the element
                startElement = ElementHierarchy.Peek() as DothtmlElementNode;
            }
        }

        private DothtmlLiteralNode ReadCData()
        {
            Assert(DothtmlTokenType.OpenCData);
            var node = new DothtmlLiteralNode()
            {
                StartPosition = Peek().StartPosition
            };
            node.Tokens.Add(Peek());
            Read();
            Assert(DothtmlTokenType.CDataBody);
            node.Tokens.Add(Peek());
            node.Escape = true;
            Read();
            Assert(DothtmlTokenType.CloseCData);
            node.Tokens.Add(Peek());
            Read();

            node.Value = string.Join(string.Empty, node.Tokens.Select(t => t.Text));
            return node;
        }

        private DothtmlLiteralNode ReadComment()
        {
            Assert(DothtmlTokenType.OpenComment);
            var node = new DothtmlLiteralNode()
            {
                IsComment = true,
                StartPosition = Peek().StartPosition
            };
            node.Tokens.Add(Peek());
            Read();
            Assert(DothtmlTokenType.CommentBody);
            var body = Peek().Text;
            node.Value = body;
            node.Tokens.Add(Peek());
            Read();
            Assert(DothtmlTokenType.CloseComment);
            node.Tokens.Add(Peek());
            Read();
            return node;
        }

        /// <summary>
        /// Reads the element.
        /// </summary>
        private DothtmlElementNode ReadElement()
        {
            var startIndex = CurrentIndex;
            var node = new DothtmlElementNode() { StartPosition = Peek().StartPosition };

            Assert(DothtmlTokenType.OpenTag);
            Read();

            if (Peek().Type == DothtmlTokenType.Slash)
            {
                Read();
                SkipWhitespace();
                node.IsClosingTag = true;
            }

            

            // element name
            Assert(DothtmlTokenType.Text);
            node.TagNameToken = Read();
            node.TagName = node.TagNameToken.Text;
            if (Peek().Type == DothtmlTokenType.Colon)
            {
                Read();

                node.TagPrefix = node.TagName;
                node.TagPrefixToken = node.TagNameToken;
                Assert(DothtmlTokenType.Text);
                node.TagNameToken = Read();
                node.TagName = node.TagNameToken.Text;
            }
            SkipWhitespace();

            // attributes
            if (!node.IsClosingTag)
            {
                while (Peek().Type == DothtmlTokenType.Text)
                {
                    var attribute = ReadAttribute();
                    attribute.ParentElement = node;
                    node.Attributes.Add(attribute);
                    SkipWhitespace();
                }

                if (Peek().Type == DothtmlTokenType.Slash)
                {
                    Read();
                    SkipWhitespace();
                    node.IsSelfClosingTag = true;
                }
            }

            Assert(DothtmlTokenType.CloseTag);
            Read();
            SkipWhitespace();

            node.Tokens.AddRange(GetTokensFrom(startIndex));
            return node;
        }



        /// <summary>
        /// Reads the attribute.
        /// </summary>
        private DothtmlAttributeNode ReadAttribute()
        {
            var startIndex = CurrentIndex;
            var attribute = new DothtmlAttributeNode() { StartPosition = Peek().StartPosition };

            // attribute name
            Assert(DothtmlTokenType.Text);
            attribute.AttributeNameToken = Read();
            attribute.AttributeName = attribute.AttributeNameToken.Text;
            if (Peek().Type == DothtmlTokenType.Colon)
            {
                Read();

                attribute.AttributePrefix = attribute.AttributeName;
                attribute.AttributePrefixToken = attribute.AttributeNameToken;
                Assert(DothtmlTokenType.Text);
                attribute.AttributeNameToken = Read();
                attribute.AttributeName = attribute.AttributeNameToken.Text;
            }
            SkipWhitespace();

            if (Peek().Type == DothtmlTokenType.Equals)
            {
                Read();
                SkipWhitespace();

                // attribute value
                if (Peek().Type == DothtmlTokenType.SingleQuote || Peek().Type == DothtmlTokenType.DoubleQuote)
                {
                    var quote = Peek().Type;
                    Read();

                    if (Peek().Type == DothtmlTokenType.OpenBinding)
                    {
                        attribute.Literal = ReadBinding();
                    }
                    else
                    {
                        Assert(DothtmlTokenType.Text);
                        attribute.Literal = new DothtmlLiteralNode() { Value = WebUtility.HtmlDecode(Peek().Text), Tokens = { Peek() }, StartPosition = Peek().StartPosition };
                        Read();
                    }

                    Assert(quote);
                    Read();
                }
                else
                {
                    Assert(DothtmlTokenType.Text);
                    attribute.Literal = new DothtmlLiteralNode() { Value = Peek().Text, Tokens = { Peek() }, StartPosition = Peek().StartPosition };
                    Read();
                }
                SkipWhitespace();
            }

            attribute.Tokens.AddRange(GetTokensFrom(startIndex));
            return attribute;
        }

        /// <summary>
        /// Reads the binding.
        /// </summary>
        private DothtmlLiteralNode ReadBinding()
        {
            var startIndex = CurrentIndex;
            var binding = new DothtmlBindingNode() { StartPosition = Peek().StartPosition };

            Assert(DothtmlTokenType.OpenBinding);
            Read();
            SkipWhitespace();

            // binding type
            Assert(DothtmlTokenType.Text);
            binding.Name = Read().Text;
            SkipWhitespace();

            Assert(DothtmlTokenType.Colon);
            Read();
            SkipWhitespace();

            // expression
            Assert(DothtmlTokenType.Text);
            binding.Value = Read().Text;
            SkipWhitespace();

            Assert(DothtmlTokenType.CloseBinding);
            Read();

            binding.Tokens.AddRange(GetTokensFrom(startIndex));
            return binding;
        }


        /// <summary>
        /// Reads the directive.
        /// </summary>
        private DothtmlDirectiveNode ReadDirective()
        {
            var startIndex = CurrentIndex;
            var node = new DothtmlDirectiveNode() { StartPosition = Peek().StartPosition };

            Assert(DothtmlTokenType.DirectiveStart);
            Read();
            SkipWhitespace();

            Assert(DothtmlTokenType.DirectiveName);
            var directiveNameToken = Read();
            node.Name = directiveNameToken.Text.Trim();

            SkipWhitespace();

            Assert(DothtmlTokenType.DirectiveValue);
            var directiveValueToken = Read();
            node.Value = directiveValueToken.Text.Trim();
            SkipWhitespace();

            node.Tokens.AddRange(GetTokensFrom(startIndex));
            return node;
        }

        protected override DothtmlTokenType WhiteSpaceToken => DothtmlTokenType.WhiteSpace;

        /// <summary>
        /// Asserts that the current token is of a specified type.
        /// </summary>
        protected bool Assert(DothtmlTokenType desiredType)
        {
            if (Peek() == null || !Peek().Type.Equals(desiredType))
            {
                throw new DotvvmInternalException($"DotVVM parser internal error! The token {desiredType} was expected!");
            }
            return true;
        }
    }
}
