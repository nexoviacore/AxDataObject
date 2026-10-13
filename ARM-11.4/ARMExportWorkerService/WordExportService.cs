using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace ARMExportWorkerService
{
    public static class WordExportService
    {
        public static string GetWordFast(string ires, string exportPDFPath, string _ivName, string _params, string _userName, string dateformat, string ivCaption, string _exeDir, string axConfigFileMapUser, string axConfigFileMapPwd, string _smartViewSettings)
        {
            string outputPath = string.Empty;
            string fileName = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(ires))
                    return "IView Result Error";

                XmlDocument xmlDoc =
                    new XmlDocument();

                xmlDoc.LoadXml(ires);

                XmlNode headRow =
                    xmlDoc.SelectSingleNode("//headrow");

                XmlNodeList dataRows =
                    xmlDoc.SelectNodes("//row");

                XmlNodeList reportHF =
                    xmlDoc.SelectNodes("//data/reporthf");

                if (headRow == null)
                    return "Invalid XML structure";

                if (dataRows == null)
                    dataRows =
                        xmlDoc.SelectNodes("//row");

                if (dataRows == null)
                    return "Invalid XML structure";

                // ----------------------------------------------------
                // Configuration
                // ----------------------------------------------------

                string wordMargin = string.Empty;
                string wordOrientation = string.Empty;
                string appTitle = string.Empty;

                bool isLandscape =
                    string.Equals(
                        wordOrientation,
                        "landscape",
                        StringComparison.OrdinalIgnoreCase);

                // ----------------------------------------------------
                // Hidden columns from Smart View settings
                // ----------------------------------------------------

                HashSet<string> hiddenColumnSet =
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);

                if (!string.IsNullOrWhiteSpace(
                        _smartViewSettings))
                {
                    hiddenColumnSet =
                        new HashSet<string>(
                            _smartViewSettings.Split(
                                new[] { ',' },
                                StringSplitOptions.RemoveEmptyEntries),
                            StringComparer.OrdinalIgnoreCase);
                }

                // ----------------------------------------------------
                // Read columns
                // ----------------------------------------------------

                List<WordColumnDefinition> columns =
                    new List<WordColumnDefinition>();

                foreach (XmlNode col in headRow.ChildNodes)
                {
                    if (col.NodeType != XmlNodeType.Element)
                        continue;

                    if (IsInternalColumn(col.Name))
                        continue;

                    bool hidden =
                        col.Name.StartsWith(
                            "hide_",
                            StringComparison.OrdinalIgnoreCase);

                    XmlAttribute hideAttribute =
                        col.Attributes?["hide"];

                    if (hideAttribute != null &&
                        string.Equals(
                            hideAttribute.Value,
                            "true",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        hidden = true;
                    }

                    if (hiddenColumnSet.Contains(col.Name))
                        hidden = true;

                    string caption =
                        col.InnerText;

                    if (string.IsNullOrWhiteSpace(caption))
                        caption = col.Name;

                    if (string.Equals(
                            caption,
                            "axp_slno",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        caption = "Sr. No.";
                    }

                    caption =
                        caption.Replace("~", "\n");

                    columns.Add(
                        new WordColumnDefinition
                        {
                            Name = col.Name,
                            Caption = StripHtmlTags(caption),
                            Hidden = hidden,
                            Width = GetColumnWidth(col),
                            Align = GetColumnAlignment(col)
                        });
                }

                // ----------------------------------------------------
                // HTML column handling
                //
                // html_xxx is displayed instead of xxx
                // ----------------------------------------------------

                HashSet<string> htmlColumns =
                    new HashSet<string>(
                        columns
                            .Where(x =>
                                x.Name.StartsWith(
                                    "html_",
                                    StringComparison.OrdinalIgnoreCase))
                            .Select(x => x.Name),
                        StringComparer.OrdinalIgnoreCase);

                foreach (WordColumnDefinition column in columns)
                {
                    if (!htmlColumns.Contains(column.Name))
                        continue;

                    column.Hidden = false;

                    string originalColumn =
                        column.Name.Substring(5);

                    WordColumnDefinition original = columns.FirstOrDefault(x => string.Equals(x.Name, originalColumn, StringComparison.OrdinalIgnoreCase));
                    if (original != null)
                        original.Hidden = true;
                }

                List<WordColumnDefinition> visibleColumns = columns.Where(x => !x.Hidden).ToList();

                if (visibleColumns.Count == 0)
                    return "No visible columns found";

                // ----------------------------------------------------
                // Output folder and file name
                // ----------------------------------------------------

                string folder = Path.Combine(exportPDFPath, "Exports", _userName);

                Directory.CreateDirectory(folder);

                string fileBase = string.IsNullOrWhiteSpace(ivCaption) ? _ivName : ivCaption;

                if (string.IsNullOrWhiteSpace(fileBase))
                    fileBase = "Export";

                fileBase = SanitizeFileName(fileBase);

                fileName = fileBase + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture) + ".docx";

                outputPath = Path.Combine(folder, fileName);

                // ----------------------------------------------------
                // Page size
                // ----------------------------------------------------

                uint pageWidth = isLandscape ? 16838U : 11906U;
                uint pageHeight = isLandscape ? 11906U : 16838U;

                int topMargin = 1080;
                int bottomMargin = 1080;

                uint leftMargin = 1080U;
                uint rightMargin = 1080U;

                // ----------------------------------------------------
                // Create Word document
                // ----------------------------------------------------

                using (
                    WordprocessingDocument document =
                        WordprocessingDocument.Create(
                            outputPath,
                            WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = document.AddMainDocumentPart();

                    mainPart.Document = new Document();

                    Body body = new Body();

                    // ------------------------------------------------
                    // Section properties
                    // ------------------------------------------------

                    SectionProperties sectionProperties = new SectionProperties();

                    PageSize pageSize =
                        new PageSize
                        {
                            Width = pageWidth,
                            Height = pageHeight
                        };

                    if (isLandscape)
                    {
                        pageSize.Orient =
                            PageOrientationValues.Landscape;
                    }

                    PageMargin pageMargin =
                        new PageMargin
                        {
                            Top =
                                new Int32Value(topMargin),

                            Bottom =
                                new Int32Value(bottomMargin),

                            Left =
                                new UInt32Value(leftMargin),

                            Right =
                                new UInt32Value(rightMargin),

                            Header =
                                new UInt32Value(360U),

                            Footer =
                                new UInt32Value(360U),

                            Gutter =
                                new UInt32Value(0U)
                        };

                    sectionProperties.Append(pageSize);
                    sectionProperties.Append(pageMargin);

                    // ------------------------------------------------
                    // Application title
                    // ------------------------------------------------

                    if (!string.IsNullOrWhiteSpace(appTitle))
                    {
                        body.Append(
                            CreateParagraph(
                                appTitle,
                                "center",
                                true,
                                10));
                    }

                    // ------------------------------------------------
                    // Report header
                    // ------------------------------------------------

                    bool headerWritten = false;

                    if (reportHF != null)
                    {
                        foreach (XmlNode hf in reportHF)
                        {
                            XmlNode headerNode =
                                hf.SelectSingleNode("header");

                            if (headerNode == null)
                                continue;

                            string firstHeaderAlign =
                                "@center";

                            foreach (XmlNode h in headerNode.ChildNodes)
                            {
                                string caption =
                                    CleanWordValue(
                                        h.SelectSingleNode("text")
                                            ?.InnerText);

                                string currentAlign =
                                    h.SelectSingleNode(
                                        "header_aline")
                                        ?.InnerText;

                                if (!string.IsNullOrWhiteSpace(
                                        currentAlign))
                                {
                                    firstHeaderAlign =
                                        currentAlign;
                                }

                                string align =
                                    string.IsNullOrWhiteSpace(
                                        currentAlign)
                                        ? firstHeaderAlign
                                        : currentAlign;

                                if (string.IsNullOrWhiteSpace(
                                        caption))
                                {
                                    continue;
                                }

                                body.Append(
                                    CreateParagraph(
                                        caption,
                                        align,
                                        true,
                                        10));

                                headerWritten = true;
                            }
                        }
                    }

                    // ------------------------------------------------
                    // Caption fallback
                    // ------------------------------------------------

                    if (!headerWritten &&
                        !string.IsNullOrWhiteSpace(ivCaption))
                    {
                        body.Append(
                            CreateParagraph(
                                CleanWordValue(ivCaption),
                                "center",
                                true,
                                14));
                    }

                    // ------------------------------------------------
                    // Parameters
                    // ------------------------------------------------

                    if (!string.IsNullOrWhiteSpace(_params) &&
                        _params.Trim() != "{}")
                    {
                        JObject json =
                            JObject.Parse(_params);

                        foreach (var parameter in json)
                        {
                            string parameterText =
                                CleanWordValue(
                                    parameter.Key) +
                                ": " +
                                CleanWordValue(
                                    parameter.Value == null
                                        ? string.Empty
                                        : parameter.Value.ToString());

                            body.Append(
                                CreateParagraph(
                                    parameterText,
                                    "left",
                                    true,
                                    8));
                        }
                    }

                    // ------------------------------------------------
                    // Main table
                    // ------------------------------------------------

                    Table table =
                        CreateWordTable(
                            visibleColumns);

                    // ------------------------------------------------
                    // Pivot group headers
                    // ------------------------------------------------

                    XmlNode pivotNode =
                        headRow.SelectSingleNode(
                            "pivotghead");

                    if (pivotNode != null)
                    {
                        AddWordPivotHeaderRow(
                            table,
                            headRow,
                            pivotNode,
                            visibleColumns);
                    }

                    // ------------------------------------------------
                    // Column headers
                    //
                    // Important:
                    // Create ONE TableRow for all columns.
                    // ------------------------------------------------

                    TableRow headerRow =
                        new TableRow();

                    // Repeat this row on every page.
                    headerRow.Append(
                        new TableRowProperties(
                            new TableHeader()));

                    foreach (
                        WordColumnDefinition column
                        in visibleColumns)
                    {
                        TableCell headerCell =
                            CreateTableCell(
                                column.Caption,
                                column.Align,
                                true,
                                true,
                                GetWordWidth(
                                    column.Width));

                        headerRow.Append(headerCell);
                    }

                    table.Append(headerRow);

                    // ------------------------------------------------
                    // Data rows
                    // ------------------------------------------------

                    foreach (XmlNode dataRow in dataRows)
                    {
                        TableRow wordRow =
                            new TableRow();

                        foreach (
                            WordColumnDefinition column
                            in visibleColumns)
                        {
                            XmlNode valueNode =
                                dataRow.SelectSingleNode(
                                    "./" + column.Name);

                            string cellText =
                                CleanWordValue(
                                    valueNode == null
                                        ? string.Empty
                                        : valueNode.InnerText);

                            TableCell dataCell =
                                CreateTableCell(
                                    cellText,
                                    column.Align,
                                    false,
                                    false,
                                    GetWordWidth(
                                        column.Width));

                            wordRow.Append(dataCell);
                        }

                        table.Append(wordRow);
                    }

                    body.Append(table);

                    // ------------------------------------------------
                    // Report footer
                    // ------------------------------------------------

                    if (reportHF != null)
                    {
                        foreach (XmlNode hf in reportHF)
                        {
                            XmlNode footerNode =
                                hf.SelectSingleNode("footer");

                            if (footerNode == null)
                                continue;

                            string firstFooterAlign =
                                "@center";

                            foreach (XmlNode f in footerNode.ChildNodes)
                            {
                                string caption =
                                    CleanWordValue(
                                        f.SelectSingleNode("text")
                                            ?.InnerText);

                                string currentAlign =
                                    f.SelectSingleNode(
                                        "footer_aline")
                                        ?.InnerText;

                                if (!string.IsNullOrWhiteSpace(
                                        currentAlign))
                                {
                                    firstFooterAlign =
                                        currentAlign;
                                }

                                string align =
                                    string.IsNullOrWhiteSpace(
                                        currentAlign)
                                        ? firstFooterAlign
                                        : currentAlign;

                                if (string.IsNullOrWhiteSpace(
                                        caption))
                                {
                                    continue;
                                }

                                body.Append(
                                    CreateParagraph(
                                        caption,
                                        align,
                                        true,
                                        10));
                            }
                        }
                    }

                    // ------------------------------------------------
                    // Printed information
                    // ------------------------------------------------

                    if (!string.IsNullOrWhiteSpace(_userName))
                    {
                        string printedBy =
                            "Printed by: " +
                            _userName +
                            " and Printed On: " +
                            DateTime.Now.ToString(
                                "dd/MM/yyyy HH:mm:ss",
                                CultureInfo.InvariantCulture);

                        body.Append(
                            CreateParagraph(
                                printedBy,
                                "right",
                                false,
                                6));
                    }

                    // SectionProperties must be the final child
                    // of Body.
                    body.Append(sectionProperties);

                    mainPart.Document.Append(body);
                    mainPart.Document.Save();
                }

                return outputPath + "," + fileName;
            }
            catch (Exception ex)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(_exeDir))
                    {
                        File.AppendAllText(
                            _exeDir,
                            "Error in Word export." +
                            Environment.NewLine +
                            "OutputPath: " +
                            outputPath +
                            Environment.NewLine +
                            "ExportPath: " +
                            exportPDFPath +
                            Environment.NewLine +
                            "Exception: " +
                            ex +
                            Environment.NewLine);
                    }
                }
                catch
                {
                    // Do not hide the original export error.
                }

                return outputPath;
            }
        }

        // ============================================================
        // CREATE WORD TABLE
        // ============================================================

        private static Table CreateWordTable(
            List<WordColumnDefinition> columns)
        {
            Table table =
                new Table();

            TableProperties tableProperties =
                new TableProperties();

            TableBorders borders =
                new TableBorders(
                    new TopBorder
                    {
                        Val = BorderValues.Single,
                        Size = 4
                    },
                    new BottomBorder
                    {
                        Val = BorderValues.Single,
                        Size = 4
                    },
                    new LeftBorder
                    {
                        Val = BorderValues.Single,
                        Size = 4
                    },
                    new RightBorder
                    {
                        Val = BorderValues.Single,
                        Size = 4
                    },
                    new InsideHorizontalBorder
                    {
                        Val = BorderValues.Single,
                        Size = 4
                    },
                    new InsideVerticalBorder
                    {
                        Val = BorderValues.Single,
                        Size = 4
                    });

            tableProperties.Append(borders);

            tableProperties.Append(
                new TableWidth
                {
                    Width = "100%",
                    Type = TableWidthUnitValues.Pct
                });

            // Fixed layout is important for column alignment.
            tableProperties.Append(
                new TableLayout
                {
                    Type = TableLayoutValues.Fixed
                });

            table.Append(tableProperties);

            TableGrid tableGrid =
                new TableGrid();

            foreach (
                WordColumnDefinition column
                in columns)
            {
                tableGrid.Append(
                    new GridColumn
                    {
                        Width =
                            GetWordWidth(
                                column.Width)
                                .ToString(
                                    CultureInfo.InvariantCulture)
                    });
            }

            table.Append(tableGrid);

            return table;
        }

        // ============================================================
        // PIVOT HEADER ROW
        // ============================================================

        private static void AddWordPivotHeaderRow(
            Table table,
            XmlNode headRow,
            XmlNode pivotNode,
            List<WordColumnDefinition> visibleColumns)
        {
            if (table == null ||
                headRow == null ||
                pivotNode == null ||
                visibleColumns == null ||
                visibleColumns.Count == 0)
            {
                return;
            }

            List<string> allColumnNames =
                new List<string>();

            foreach (XmlNode col in headRow.ChildNodes)
            {
                if (col.NodeType != XmlNodeType.Element)
                    continue;

                if (IsInternalColumn(col.Name))
                    continue;

                string columnName =
                    col.Name;

                string nameAttribute =
                    col.Attributes?["name"]?.Value;

                if (!string.IsNullOrWhiteSpace(
                        nameAttribute))
                {
                    columnName =
                        nameAttribute;
                }

                allColumnNames.Add(columnName);
            }

            Dictionary<int, int> wordIndexMap =
                new Dictionary<int, int>();

            int wordColumnIndex = 1;

            for (int i = 0;
                 i < allColumnNames.Count;
                 i++)
            {
                string originalColumnName =
                    allColumnNames[i];

                WordColumnDefinition visibleColumn =
                    visibleColumns.FirstOrDefault(
                        column =>
                            column != null &&
                            string.Equals(
                                column.Name,
                                originalColumnName,
                                StringComparison.OrdinalIgnoreCase));

                if (visibleColumn == null)
                    continue;

                wordIndexMap[i + 1] =
                    wordColumnIndex++;

            }

            XmlDocument pivotXml =
                new XmlDocument();

            pivotXml.LoadXml(
                "<pivot>" +
                pivotNode.InnerXml +
                "</pivot>");

            XmlNodeList pivotHeaders =
                pivotXml.SelectNodes("//head");

            if (pivotHeaders == null ||
                pivotHeaders.Count == 0)
            {
                return;
            }

            int currentWordColumn = 1;

            TableRow pivotRow =
                new TableRow();

            foreach (XmlNode pivotHeader in pivotHeaders)
            {
                int startNumber = 0;
                int endNumber = 0;

                int.TryParse(
                    pivotHeader.SelectSingleNode("snno")
                        ?.InnerText,
                    out startNumber);

                int.TryParse(
                    pivotHeader.SelectSingleNode("enno")
                        ?.InnerText,
                    out endNumber);

                string groupHeader =
                    CleanWordValue(
                        pivotHeader.SelectSingleNode("ghead")
                            ?.InnerText);

                if (string.IsNullOrWhiteSpace(
                        groupHeader))
                {
                    continue;
                }

                List<int> wordColumns =
                    wordIndexMap
                        .Where(
                            item =>
                                item.Key >= startNumber &&
                                item.Key <= endNumber)
                        .Select(
                            item => item.Value)
                        .OrderBy(
                            value => value)
                        .ToList();

                if (wordColumns.Count == 0)
                    continue;

                int firstWordColumn =
                    wordColumns.First();

                int lastWordColumn =
                    wordColumns.Last();

                // Fill blank cells before the pivot group.
                while (
                    currentWordColumn <
                    firstWordColumn)
                {
                    pivotRow.Append(
                        CreateTableCell(
                            string.Empty,
                            "center",
                            true,
                            true,
                            GetWordWidth(100)));

                    currentWordColumn++;
                }

                int colspan =
                    lastWordColumn -
                    firstWordColumn +
                    1;

                TableCell pivotCell =
                    CreateTableCell(
                        groupHeader,
                        "center",
                        true,
                        true,
                        GetWordWidth(100));

                TableCellProperties properties =
                    pivotCell.GetFirstChild<
                        TableCellProperties>();

                if (properties == null)
                {
                    properties =
                        new TableCellProperties();

                    pivotCell.PrependChild(
                        properties);
                }

                properties.Append(
                    new GridSpan
                    {
                        Val = colspan
                    });

                pivotRow.Append(pivotCell);

                currentWordColumn =
                    lastWordColumn + 1;
            }

            // Fill remaining cells.
            while (
                currentWordColumn <=
                visibleColumns.Count)
            {
                pivotRow.Append(
                    CreateTableCell(
                        string.Empty,
                        "center",
                        true,
                        true,
                        GetWordWidth(100)));

                currentWordColumn++;
            }

            table.Append(pivotRow);
        }

        // ============================================================
        // CREATE TABLE CELL
        // ============================================================

        private static TableCell CreateTableCell(
            string text,
            string alignment,
            bool bold,
            bool grayBackground,
            uint width)
        {
            TableCell cell =
                new TableCell();

            TableCellProperties cellProperties =
                new TableCellProperties();

            cellProperties.Append(
                new TableCellWidth
                {
                    Width =
                        width.ToString(
                            CultureInfo.InvariantCulture),

                    Type =
                        TableWidthUnitValues.Dxa
                });

            if (grayBackground)
            {
                cellProperties.Append(
                    new Shading
                    {
                        Val =
                            ShadingPatternValues.Clear,

                        Fill = "E6E6E6"
                    });
            }

            cellProperties.Append(
                new TableCellVerticalAlignment
                {
                    Val =
                        TableVerticalAlignmentValues.Center
                });

            cell.Append(cellProperties);

            Paragraph paragraph =
                new Paragraph();

            ParagraphProperties paragraphProperties =
                new ParagraphProperties();

            paragraphProperties.Append(
                new Justification
                {
                    Val =
                        GetJustification(alignment)
                });

            paragraph.Append(
                paragraphProperties);

            Run run =
                new Run();

            RunProperties runProperties =
                new RunProperties();

            // 8 points = 16 half-points.
            runProperties.Append(
                new FontSize
                {
                    Val = "16"
                });

            if (bold)
                runProperties.Append(
                    new Bold());

            run.Append(runProperties);

            string[] lines =
                (text ?? string.Empty).Split(
                    new[] { "\n" },
                    StringSplitOptions.None);

            for (int i = 0;
                 i < lines.Length;
                 i++)
            {
                run.Append(
                    new Text(lines[i])
                    {
                        Space =
                            SpaceProcessingModeValues.Preserve
                    });

                if (i < lines.Length - 1)
                    run.Append(
                        new Break());
            }

            paragraph.Append(run);
            cell.Append(paragraph);

            return cell;
        }

        // ============================================================
        // CREATE PARAGRAPH
        // ============================================================

        private static Paragraph CreateParagraph(
            string text,
            string alignment,
            bool bold,
            int fontSize)
        {
            Paragraph paragraph =
                new Paragraph();

            ParagraphProperties paragraphProperties =
                new ParagraphProperties();

            paragraphProperties.Append(
                new Justification
                {
                    Val =
                        GetJustification(alignment)
                });

            paragraph.Append(
                paragraphProperties);

            Run run =
                new Run();

            RunProperties runProperties =
                new RunProperties();

            runProperties.Append(
                new FontSize
                {
                    Val =
                        (fontSize * 2).ToString(
                            CultureInfo.InvariantCulture)
                });

            if (bold)
                runProperties.Append(
                    new Bold());

            run.Append(runProperties);

            string[] lines =
                (text ?? string.Empty).Split(
                    new[] { "\n" },
                    StringSplitOptions.None);

            for (int i = 0;
                 i < lines.Length;
                 i++)
            {
                run.Append(
                    new Text(lines[i])
                    {
                        Space =
                            SpaceProcessingModeValues.Preserve
                    });

                if (i < lines.Length - 1)
                    run.Append(
                        new Break());
            }

            paragraph.Append(run);

            return paragraph;
        }

        // ============================================================
        // ALIGNMENT
        // ============================================================

        private static JustificationValues GetJustification(
            string alignment)
        {
            if (string.Equals(
                    alignment,
                    "right",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    alignment,
                    "@right",
                    StringComparison.OrdinalIgnoreCase))
            {
                return JustificationValues.Right;
            }

            if (string.Equals(
                    alignment,
                    "center",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    alignment,
                    "@center",
                    StringComparison.OrdinalIgnoreCase))
            {
                return JustificationValues.Center;
            }

            return JustificationValues.Left;
        }

        // ============================================================
        // COLUMN WIDTH
        // ============================================================

        private static uint GetWordWidth(
            int width)
        {
            if (width <= 0)
                width = 100;

            // Word width is measured in twips.
            // Keep every column within a reasonable range.
            return (uint)Math.Max(
                500,
                Math.Min(
                    3000,
                    width * 5));
        }

        private static int GetColumnWidth(
            XmlNode column)
        {
            XmlAttribute widthAttribute =
                column.Attributes?["width"];

            int width;

            if (widthAttribute != null &&
                int.TryParse(
                    widthAttribute.Value,
                    out width))
            {
                return width;
            }

            return 100;
        }

        private static string GetColumnAlignment(
            XmlNode column)
        {
            XmlAttribute alignAttribute =
                column.Attributes?["align"];

            if (alignAttribute != null &&
                !string.IsNullOrWhiteSpace(
                    alignAttribute.Value))
            {
                return alignAttribute.Value;
            }

            return "left";
        }

        // ============================================================
        // INTERNAL COLUMNS
        // ============================================================

        private static bool IsInternalColumn(
            string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return true;

            return
                string.Equals(
                    name,
                    "axrowtype",
                    StringComparison.OrdinalIgnoreCase) ||

                string.Equals(
                    name,
                    "axp__font",
                    StringComparison.OrdinalIgnoreCase) ||

                string.Equals(
                    name,
                    "axp__color",
                    StringComparison.OrdinalIgnoreCase) ||

                string.Equals(
                    name,
                    "axrowoptions",
                    StringComparison.OrdinalIgnoreCase) ||

                string.Equals(
                    name,
                    "rowno",
                    StringComparison.OrdinalIgnoreCase) ||

                string.Equals(
                    name,
                    "pivotghead",
                    StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        // CLEAN HTML / XML VALUE
        // ============================================================

        private static string CleanWordValue(
            string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string previous;

            do
            {
                previous = input;

                input =
                    System.Net.WebUtility.HtmlDecode(
                        input);
            }
            while (!string.Equals(
                input,
                previous,
                StringComparison.Ordinal));

            HtmlDocument htmlDocument =
                new HtmlDocument();

            htmlDocument.LoadHtml(input);

            // Remove MS Office conditional comments.
            foreach (
                HtmlNode comment
                in htmlDocument.DocumentNode
                    .SelectNodes("//comment()") ??
                Enumerable.Empty<HtmlNode>())
            {
                if (comment.InnerHtml.IndexOf(
                        "if gte mso",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    comment.Remove();
                }
            }

            // Remove style and script nodes.
            foreach (
                HtmlNode node
                in htmlDocument.DocumentNode
                    .SelectNodes("//style|//script") ??
                Enumerable.Empty<HtmlNode>())
            {
                node.Remove();
            }

            // Convert <br> to line breaks.
            foreach (
                HtmlNode br
                in htmlDocument.DocumentNode
                    .SelectNodes("//br") ??
                Enumerable.Empty<HtmlNode>())
            {
                if (br.ParentNode != null)
                {
                    br.ParentNode.ReplaceChild(
                        htmlDocument.CreateTextNode(
                            "\n"),
                        br);
                }
            }

            // Keep only visible anchor text.
            foreach (
                HtmlNode anchor
                in htmlDocument.DocumentNode
                    .SelectNodes("//a") ??
                Enumerable.Empty<HtmlNode>())
            {
                if (anchor.ParentNode != null)
                {
                    anchor.ParentNode.ReplaceChild(
                        htmlDocument.CreateTextNode(
                            anchor.InnerText),
                        anchor);
                }
            }

            // Remove formatting tags while preserving text.
            foreach (
                HtmlNode node
                in htmlDocument.DocumentNode
                    .SelectNodes(
                        "//span|//font|//b|//strong|//p|//li|//ul|//ol") ??
                Enumerable.Empty<HtmlNode>())
            {
                if (node.ParentNode != null)
                {
                    node.ParentNode.ReplaceChild(
                        htmlDocument.CreateTextNode(
                            node.InnerText),
                        node);
                }
            }

            string clean =
                htmlDocument.DocumentNode.InnerText;

            clean =
                clean.Replace(
                    "~",
                    "\n");

            clean =
                Regex.Replace(
                    clean,
                    @"[ \t]+",
                    " ");

            clean =
                Regex.Replace(
                    clean,
                    @"\r\n|\r",
                    "\n");

            clean =
                Regex.Replace(
                    clean,
                    @"\n{3,}",
                    "\n\n");

            return clean.Trim();
        }

        // ============================================================
        // STRIP HTML TAGS
        // ============================================================

        private static string StripHtmlTags(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return Regex.Replace(
                value,
                "<.*?>",
                string.Empty).Trim();
        }

        // ============================================================
        // SANITIZE FILE NAME
        // ============================================================

        private static string SanitizeFileName(
            string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Export";

            foreach (
                char invalidChar
                in Path.GetInvalidFileNameChars())
            {
                fileName =
                    fileName.Replace(
                        invalidChar,
                        '_');
            }

            return fileName
                .Replace("/", "_")
                .Replace("\\", "_");
        }

        // ============================================================
        // COLUMN DEFINITION
        // ============================================================

        private sealed class WordColumnDefinition
        {
            public string Name { get; set; }

            public string Caption { get; set; }

            public bool Hidden { get; set; }

            public int Width { get; set; }

            public string Align { get; set; }
        }
    }
}