using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;

namespace ARMExportWorkerService
{
    public static class PdfExportService
    {
        public static string GetPDFFast(string ires, string exportPDFPath, string _ivName, string _params, string _userName, string dateformat, string ivCaption, string _exeDir, string axConfigFileMapUser, string axConfigFileMapPwd, string _smartViewSettings)
        {
            string outputPath = string.Empty;
            string pdfMargin = "";
            string pdfOrientation = "";
            string appTitle = "";
            try
            {
                if (string.IsNullOrWhiteSpace(ires))
                    return "IView Result Error";

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(ires);

                XmlNode headRow = xmlDoc.SelectSingleNode("//headrow");
                XmlNodeList dataRows = xmlDoc.SelectNodes("//row");
                XmlNodeList reportHF = xmlDoc.SelectNodes("//data/reporthf");

                if (headRow == null || dataRows == null)
                    return "Invalid XML structure";

                // ----------------------------------------------------
                // Hidden columns from Smart View settings
                // ----------------------------------------------------
                HashSet<string> hiddenColumnSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(_smartViewSettings))
                {
                    hiddenColumnSet = new HashSet<string>(_smartViewSettings.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
                }

                // ----------------------------------------------------
                // Read columns
                // ----------------------------------------------------
                List<PdfColumnDefinition> columns = new List<PdfColumnDefinition>();

                foreach (XmlNode col in headRow.ChildNodes)
                {
                    if (col.Name.Equals("axrowtype", StringComparison.OrdinalIgnoreCase) || col.Name.Equals("axp__font", StringComparison.OrdinalIgnoreCase) || col.Name.Equals("axp__color", StringComparison.OrdinalIgnoreCase) || col.Name.Equals("axrowoptions", StringComparison.OrdinalIgnoreCase) || col.Name.Equals("rowno", StringComparison.OrdinalIgnoreCase) || col.Name.Equals("pivotghead", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    bool hidden = col.Name.StartsWith("hide_", StringComparison.OrdinalIgnoreCase); if (col.Attributes?["hide"] != null && col.Attributes["hide"].Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        hidden = true;
                    }

                    if (hiddenColumnSet.Contains(col.Name))
                        hidden = true;

                    string caption = col.InnerText;

                    if (string.IsNullOrWhiteSpace(caption))
                        caption = col.Name;

                    if (caption.Equals("axp_slno", StringComparison.OrdinalIgnoreCase))
                    {
                        caption = "Sr. No.";
                    }

                    if (caption.Contains("~"))
                        caption = caption.Replace("~", "\n");

                    columns.Add(new PdfColumnDefinition
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
                // html_xxx is displayed instead of xxx
                // ----------------------------------------------------
                HashSet<string> htmlColumns = new HashSet<string>(columns.Where(x => x.Name.StartsWith("html_", StringComparison.OrdinalIgnoreCase)).Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
                foreach (PdfColumnDefinition column in columns)
                {
                    if (htmlColumns.Contains(column.Name))
                    {
                        column.Hidden = false;
                        string originalColumn = column.Name.Substring(5);
                        PdfColumnDefinition original = columns.FirstOrDefault(x => x.Name.Equals(originalColumn, StringComparison.OrdinalIgnoreCase));
                        if (original != null)
                            original.Hidden = true;
                    }
                }
                List<PdfColumnDefinition> visibleColumns = columns.Where(x => !x.Hidden).ToList();
                if (visibleColumns.Count == 0)
                    return "No visible columns found";

                // ----------------------------------------------------
                // PDF folder and file name
                // ----------------------------------------------------
                string folder = Path.Combine(exportPDFPath, "Exports", _userName);
                Directory.CreateDirectory(folder);
                string fileBase = string.IsNullOrWhiteSpace(ivCaption) ? _ivName : ivCaption;
                if (string.IsNullOrWhiteSpace(fileBase))
                    fileBase = "Export";
                fileBase = SanitizeFileName(fileBase);
                string fileName = fileBase + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture) + ".pdf";
                outputPath = Path.Combine(folder, fileName);

                // ----------------------------------------------------
                // PDF margins
                // ----------------------------------------------------
                float top = 54f;
                float bottom = 54f;
                float left = 36f;
                float right = 36f;

                if (!string.IsNullOrWhiteSpace(pdfMargin))
                {
                    try
                    {
                        Dictionary<string, string> dimensions = JsonConvert.DeserializeObject<Dictionary<string, string>>(pdfMargin);
                        if (dimensions != null)
                        {
                            if (dimensions.ContainsKey("top") && float.TryParse(dimensions["top"], out float topValue))
                            {
                                top *= topValue;
                            }

                            if (dimensions.ContainsKey("bottom") && float.TryParse(dimensions["bottom"], out float bottomValue))
                            {
                                bottom *= bottomValue;
                            }

                            if (dimensions.ContainsKey("left") && float.TryParse(dimensions["left"], out float leftValue))
                            {
                                left *= leftValue;
                            }

                            if (dimensions.ContainsKey("right") && float.TryParse(dimensions["right"], out float rightValue))
                            {
                                right *= rightValue;
                            }
                        }
                    }
                    catch
                    {
                        // Keep default margins if JSON is invalid
                    }
                }

                bool isLandscape = string.Equals(pdfOrientation, "landscape", StringComparison.OrdinalIgnoreCase);
                Rectangle pageSize = isLandscape ? PageSize.A4.Rotate() : PageSize.A4;

                // ----------------------------------------------------
                // Create PDF document
                // ----------------------------------------------------
                using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (Document document = new Document(pageSize, left, right, top, bottom))
                    {
                        PdfWriter.GetInstance(document, fileStream);
                        document.Open();
                        // ------------------------------------------------
                        // PDF table
                        // ------------------------------------------------
                        PdfPTable table = new PdfPTable(visibleColumns.Count);
                        table.WidthPercentage = 100;
                        table.SpacingBefore = 8f;
                        table.SpacingAfter = 8f;

                        // Set column widths
                        float[] widths = visibleColumns.Select(x => x.Width > 0 ? x.Width : 100f).ToArray();
                        table.SetWidths(widths);

                        // ------------------------------------------------
                        // Application title
                        // ------------------------------------------------
                        if (!string.IsNullOrWhiteSpace(appTitle))
                        {
                            Paragraph titleParagraph = new Paragraph(appTitle, FontFactory.GetFont(FontFactory.HELVETICA, 10, iTextSharp.text.Font.BOLD));
                            titleParagraph.Alignment = Element.ALIGN_CENTER;
                            document.Add(titleParagraph);
                        }

                        //// ------------------------------------------------
                        //// Report header
                        //// ------------------------------------------------                       
                        bool headerWritten = false;
                        if (reportHF != null)
                        {
                            foreach (XmlNode hf in reportHF)
                            {
                                XmlNode headerNode = hf.SelectSingleNode("header");

                                if (headerNode == null)
                                    continue;

                                string firstHeaderAlign = "@center";

                                foreach (XmlNode h in headerNode.ChildNodes)
                                {
                                    string caption =
                                        CleanPdfValue(
                                            h.SelectSingleNode("text")?.InnerText);

                                    string currentAlign =
                                        h.SelectSingleNode("header_aline")?.InnerText;

                                    // Store the first header_aline value found
                                    if (!string.IsNullOrWhiteSpace(currentAlign))
                                    {
                                        firstHeaderAlign = currentAlign;
                                    }

                                    // If header_aline is missing, use the previous alignment
                                    string align =
                                        string.IsNullOrWhiteSpace(currentAlign)
                                            ? firstHeaderAlign
                                            : currentAlign;

                                    if (string.IsNullOrWhiteSpace(caption))
                                        continue;

                                    PdfPCell headerCell =
                                        new PdfPCell(
                                            new Phrase(
                                                caption,
                                                FontFactory.GetFont(
                                                    FontFactory.HELVETICA,
                                                    10,
                                                    iTextSharp.text.Font.BOLD)));

                                    // Merge header across all visible columns
                                    headerCell.Colspan = visibleColumns.Count;

                                    // Same alignment behavior as Excel
                                    if (align == "" || align == "@center")
                                    {
                                        headerCell.HorizontalAlignment =
                                            Element.ALIGN_CENTER;
                                    }
                                    else if (align == "@left")
                                    {
                                        headerCell.HorizontalAlignment =
                                            Element.ALIGN_LEFT;
                                    }
                                    else if (align == "@right")
                                    {
                                        headerCell.HorizontalAlignment =
                                            Element.ALIGN_RIGHT;
                                    }

                                    headerCell.VerticalAlignment =
                                        Element.ALIGN_MIDDLE;

                                    headerCell.Border = Rectangle.NO_BORDER;
                                    headerCell.PaddingBottom = 5f;

                                    table.AddCell(headerCell);

                                    headerWritten = true;
                                }
                            }
                        }
                        if (!headerWritten && !string.IsNullOrWhiteSpace(ivCaption))
                        {
                            PdfPCell titleCell = new PdfPCell(new Phrase(CleanPdfValue(ivCaption), FontFactory.GetFont(FontFactory.HELVETICA, 14, iTextSharp.text.Font.BOLD)));
                            titleCell.Colspan = visibleColumns.Count;
                            titleCell.HorizontalAlignment = Element.ALIGN_CENTER;
                            titleCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            titleCell.Border = Rectangle.NO_BORDER;
                            titleCell.PaddingBottom = 8f;
                            table.AddCell(titleCell);
                        }

                        // ============================================================
                        // PARAMETERS - Same logic as GetExcelFast
                        // ============================================================
                        if (!string.IsNullOrWhiteSpace(_params) && _params != "{}")
                        {
                            JObject json = JObject.Parse(_params);

                            foreach (var p in json)
                            {
                                string parameterText =
                                    $"{CleanPdfValue(p.Key)}: " +
                                    $"{CleanPdfValue(p.Value?.ToString())}";

                                PdfPCell parameterCell =
                                    new PdfPCell(
                                        new Phrase(
                                            parameterText,
                                            FontFactory.GetFont(
                                                FontFactory.HELVETICA,
                                                8,
                                                iTextSharp.text.Font.BOLD)));

                                parameterCell.Colspan = visibleColumns.Count;
                                parameterCell.HorizontalAlignment = Element.ALIGN_LEFT;
                                parameterCell.Border = Rectangle.NO_BORDER;
                                parameterCell.PaddingBottom = 3f;

                                table.AddCell(parameterCell);
                            }
                        }

                        //------------------------------------------------------------------
                        // Add pivot headers before column headers
                        //------------------------------------------------------------------

                        XmlNode pivotNode =
                            headRow.SelectSingleNode("pivotghead");

                        if (pivotNode != null)
                        {
                            AddPdfPivotHeaderRow(
                                table,
                                headRow,
                                pivotNode,
                                visibleColumns);
                        }

                        // ============================================================
                        // COLUMN HEADERS
                        // ============================================================
                        foreach (PdfColumnDefinition column in visibleColumns)
                        {
                            PdfPCell headerCell = new PdfPCell(new Phrase(column.Caption, FontFactory.GetFont(FontFactory.HELVETICA, 8, iTextSharp.text.Font.BOLD)));

                            headerCell.HorizontalAlignment =
                                GetPdfAlignment(column.Align);

                            headerCell.VerticalAlignment =
                                Element.ALIGN_MIDDLE;

                            headerCell.BackgroundColor =
                                new BaseColor(230, 230, 230);

                            headerCell.Padding = 4f;

                            table.AddCell(headerCell);
                        }

                        // ============================================================
                        // DATA
                        // ============================================================
                        foreach (XmlNode dataRow in dataRows)
                        {
                            foreach (PdfColumnDefinition column in visibleColumns)
                            {
                                XmlNode valueNode =
                                    dataRow.SelectSingleNode("./" + column.Name);

                                string cellText =
                                    CleanPdfValue(valueNode?.InnerText);

                                if (cellText.Contains("~"))
                                    cellText = cellText.Replace("~", "\n");

                                PdfPCell dataCell =
                                    new PdfPCell(
                                        new Phrase(
                                            cellText,
                                            FontFactory.GetFont(
                                                FontFactory.HELVETICA,
                                                8,
                                                iTextSharp.text.Font.NORMAL)));

                                dataCell.HorizontalAlignment =
                                    GetPdfAlignment(column.Align);

                                dataCell.VerticalAlignment =
                                    Element.ALIGN_MIDDLE;

                                dataCell.Padding = 3f;

                                table.AddCell(dataCell);
                            }
                        }

                        // ============================================================
                        // REPORT FOOTER - Same logic as GetExcelFast
                        // ============================================================
                        if (reportHF != null)
                        {
                            foreach (XmlNode hf in reportHF)
                            {
                                XmlNode footerNode =
                                    hf.SelectSingleNode("footer");

                                if (footerNode == null)
                                    continue;

                                string firstFooterAlign = "@center";

                                foreach (XmlNode f in footerNode.ChildNodes)
                                {
                                    string caption =
                                        CleanPdfValue(
                                            f.SelectSingleNode("text")?.InnerText);

                                    string currentAlign =
                                        f.SelectSingleNode("footer_aline")?.InnerText;

                                    // Store the latest non-empty footer alignment
                                    if (!string.IsNullOrWhiteSpace(currentAlign))
                                    {
                                        firstFooterAlign = currentAlign;
                                    }

                                    // If footer_aline is missing, use the previous alignment
                                    string align =
                                        string.IsNullOrWhiteSpace(currentAlign)
                                            ? firstFooterAlign
                                            : currentAlign;

                                    if (string.IsNullOrWhiteSpace(caption))
                                        continue;

                                    PdfPCell footerCell =
                                        new PdfPCell(
                                            new Phrase(
                                                caption,
                                                FontFactory.GetFont(
                                                    FontFactory.HELVETICA,
                                                    8,
                                                    iTextSharp.text.Font.BOLD)));

                                    footerCell.Colspan = visibleColumns.Count;

                                    // Same alignment behavior as Excel
                                    if (align == "" || align == "@center")
                                    {
                                        footerCell.HorizontalAlignment =
                                            Element.ALIGN_CENTER;
                                    }
                                    else if (align == "@left")
                                    {
                                        footerCell.HorizontalAlignment =
                                            Element.ALIGN_LEFT;
                                    }
                                    else if (align == "@right")
                                    {
                                        footerCell.HorizontalAlignment =
                                            Element.ALIGN_RIGHT;
                                    }

                                    footerCell.VerticalAlignment =
                                        Element.ALIGN_MIDDLE;

                                    footerCell.Border = Rectangle.NO_BORDER;
                                    footerCell.PaddingTop = 5f;

                                    table.AddCell(footerCell);
                                }
                            }
                        }
                        document.Add(table);
                        // ------------------------------------------------
                        // Printed information
                        // ------------------------------------------------
                        if (!string.IsNullOrWhiteSpace(_userName))
                        {
                            Paragraph printedBy = new Paragraph("Printed by: " + _userName + " and Printed On: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture), FontFactory.GetFont(FontFactory.HELVETICA, 6, iTextSharp.text.Font.NORMAL));
                            printedBy.Alignment = Element.ALIGN_RIGHT;
                            document.Add(printedBy);
                        }
                        document.Close();
                    }
                }
                outputPath = outputPath + "," + fileName;
            }
            catch (Exception ex)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(_exeDir))
                    {
                        File.AppendAllText(_exeDir, "Error in PDF export. " + "OutputPath: " + outputPath + " ExportPath: " + exportPDFPath + " Exception: " + ex + Environment.NewLine);
                    }
                }
                catch
                { }
            }
            return outputPath;
        }
        private static void AddPdfPivotHeaderRow(PdfPTable table, XmlNode headRow, XmlNode pivotNode, List<PdfColumnDefinition> visibleColumns)
        {
            if (table == null ||
                headRow == null ||
                pivotNode == null ||
                visibleColumns == null ||
                visibleColumns.Count == 0)
            {
                return;
            }

            //------------------------------------------------------------------
            // Build original XML column order.
            //------------------------------------------------------------------

            List<string> allColumnNames =
                new List<string>();

            foreach (XmlNode col in headRow.ChildNodes)
            {
                if (col.NodeType != XmlNodeType.Element)
                    continue;

                if (col.Name.Equals(
                        "axrowtype",
                        StringComparison.OrdinalIgnoreCase) ||
                    col.Name.Equals(
                        "axp__font",
                        StringComparison.OrdinalIgnoreCase) ||
                    col.Name.Equals(
                        "axp__color",
                        StringComparison.OrdinalIgnoreCase) ||
                    col.Name.Equals(
                        "axrowoptions",
                        StringComparison.OrdinalIgnoreCase) ||
                    col.Name.Equals(
                        "rowno",
                        StringComparison.OrdinalIgnoreCase) ||
                    col.Name.Equals(
                        "pivotghead",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string columnName =
                    col.Name;

                string nameAttribute =
                    col.Attributes?["name"]?.Value;

                if (!string.IsNullOrWhiteSpace(nameAttribute))
                {
                    columnName =
                        nameAttribute;
                }

                allColumnNames.Add(columnName);
            }

            //------------------------------------------------------------------
            // Original XML column number -> visible PDF column number
            //------------------------------------------------------------------

            Dictionary<int, int> pdfIndexMap =
                new Dictionary<int, int>();

            int pdfColumnIndex = 1;

            for (int i = 0; i < allColumnNames.Count; i++)
            {
                string originalColumnName =
                    allColumnNames[i];

                PdfColumnDefinition visibleColumn =
                    visibleColumns.FirstOrDefault(
                        column =>
                            column != null &&
                            column.Name.Equals(
                                originalColumnName,
                                StringComparison.OrdinalIgnoreCase));

                if (visibleColumn == null)
                    continue;

                pdfIndexMap[i + 1] =
                    pdfColumnIndex++;
            }

            //------------------------------------------------------------------
            // Load pivot XML
            //------------------------------------------------------------------

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

            //------------------------------------------------------------------
            // Track current PDF column.
            //------------------------------------------------------------------

            int currentPdfColumn = 1;

            foreach (XmlNode pivotHeader in pivotHeaders)
            {
                int startNumber = 0;
                int endNumber = 0;

                int.TryParse(
                    pivotHeader.SelectSingleNode("snno")?.InnerText,
                    out startNumber);

                int.TryParse(
                    pivotHeader.SelectSingleNode("enno")?.InnerText,
                    out endNumber);

                string groupHeader =
                    pivotHeader.SelectSingleNode("ghead")?.InnerText;

                groupHeader =
                    CleanPdfValue(groupHeader);

                if (string.IsNullOrWhiteSpace(groupHeader))
                    continue;

                //------------------------------------------------------------------
                // Get visible PDF columns for this pivot group.
                //------------------------------------------------------------------

                List<int> pdfColumns =
                    pdfIndexMap
                        .Where(item =>
                            item.Key >= startNumber &&
                            item.Key <= endNumber)
                        .Select(item => item.Value)
                        .OrderBy(value => value)
                        .ToList();

                if (pdfColumns.Count == 0)
                    continue;

                int firstPdfColumn =
                    pdfColumns.First();

                int lastPdfColumn =
                    pdfColumns.Last();

                //------------------------------------------------------------------
                // Fill blank cells if there is a gap before this group.
                //------------------------------------------------------------------

                while (currentPdfColumn < firstPdfColumn)
                {
                    PdfPCell blankCell =
                        new PdfPCell(
                            new Phrase(
                                string.Empty,
                                FontFactory.GetFont(
                                    FontFactory.HELVETICA,
                                    7)));

                    blankCell.HorizontalAlignment =
                        Element.ALIGN_CENTER;

                    blankCell.VerticalAlignment =
                        Element.ALIGN_MIDDLE;

                    blankCell.BackgroundColor =
                        new BaseColor(230, 230, 230);

                    blankCell.Border =
                        Rectangle.BOX;

                    table.AddCell(blankCell);

                    currentPdfColumn++;
                }

                //------------------------------------------------------------------
                // Add pivot group cell.
                //------------------------------------------------------------------

                int colspan =
                    lastPdfColumn -
                    firstPdfColumn +
                    1;

                PdfPCell pivotCell =
                    new PdfPCell(
                        new Phrase(
                            groupHeader,
                            FontFactory.GetFont(
                                FontFactory.HELVETICA,
                                8,
                                Font.BOLD)));

                pivotCell.Colspan =
                    colspan;

                pivotCell.HorizontalAlignment =
                    Element.ALIGN_CENTER;

                pivotCell.VerticalAlignment =
                    Element.ALIGN_MIDDLE;

                pivotCell.PaddingTop = 4f;
                pivotCell.PaddingBottom = 4f;
                pivotCell.NoWrap = false;

                pivotCell.BackgroundColor =
                    new BaseColor(230, 230, 230);

                pivotCell.Border =
                    Rectangle.BOX;

                table.AddCell(pivotCell);

                currentPdfColumn =
                    lastPdfColumn + 1;
            }

            //------------------------------------------------------------------
            // Fill remaining cells to complete the PDF row.
            //------------------------------------------------------------------

            while (currentPdfColumn <= visibleColumns.Count)
            {
                PdfPCell blankCell =
                    new PdfPCell(
                        new Phrase(
                            string.Empty,
                            FontFactory.GetFont(
                                FontFactory.HELVETICA,
                                7)));

                blankCell.HorizontalAlignment =
                    Element.ALIGN_CENTER;

                blankCell.VerticalAlignment =
                    Element.ALIGN_MIDDLE;

                blankCell.BackgroundColor =
                    new BaseColor(230, 230, 230);

                blankCell.Border =
                    Rectangle.BOX;

                table.AddCell(blankCell);

                currentPdfColumn++;
            }
        }
        private static string CleanPdfValue(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string previous;

            do
            {
                previous = input;
                input = WebUtility.HtmlDecode(input);
            }
            while (input != previous);

            var doc = new HtmlDocument();
            doc.LoadHtml(input);

            // Remove MS Office conditional comments
            foreach (var comment in doc.DocumentNode
                         .SelectNodes("//comment()") ?? Enumerable.Empty<HtmlNode>())
            {
                if (comment.InnerHtml.Contains("if gte mso",
                                               StringComparison.OrdinalIgnoreCase))
                {
                    comment.Remove();
                }
            }

            // Remove style and script nodes
            foreach (var node in doc.DocumentNode
                         .SelectNodes("//style|//script") ?? Enumerable.Empty<HtmlNode>())
            {
                node.Remove();
            }

            // Convert <br> to a PDF line break
            foreach (var br in doc.DocumentNode
                         .SelectNodes("//br") ?? Enumerable.Empty<HtmlNode>())
            {
                br.ParentNode.ReplaceChild(
                    doc.CreateTextNode("\n"),
                    br);
            }

            // Keep only visible anchor text
            foreach (var anchor in doc.DocumentNode
                         .SelectNodes("//a") ?? Enumerable.Empty<HtmlNode>())
            {
                anchor.ParentNode.ReplaceChild(
                    doc.CreateTextNode(anchor.InnerText),
                    anchor);
            }

            // Remove formatting tags but preserve their content
            foreach (var node in doc.DocumentNode
                         .SelectNodes("//span|//font|//b|//strong|//p|//li|//ul|//ol")
                         ?? Enumerable.Empty<HtmlNode>())
            {
                node.ParentNode.ReplaceChild(
                    doc.CreateTextNode(node.InnerText),
                    node);
            }

            string clean = doc.DocumentNode.InnerText;

            // Preserve explicit line breaks
            clean = clean.Replace("~", "\n");

            // Normalize spaces but preserve line breaks
            clean = Regex.Replace(clean, @"[ \t]+", " ");
            clean = Regex.Replace(clean, @"\r\n|\r", "\n");
            clean = Regex.Replace(clean, @"\n{3,}", "\n\n");

            return clean.Trim();
        }
        private static int GetColumnWidth(XmlNode column)
        {
            if (column.Attributes?["width"] != null &&
                int.TryParse(
                    column.Attributes["width"].Value,
                    out int width))
            {
                return width;
            }

            return 100;
        }

        private static string GetColumnAlignment(XmlNode column)
        {
            return column.Attributes?["align"]?.Value ?? "left";
        }

        private static int GetPdfAlignment(string alignment)
        {
            if (string.Equals(
                    alignment,
                    "right",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(alignment, "@right",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Element.ALIGN_RIGHT;
            }

            if (string.Equals(
                    alignment,
                    "center",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(alignment, "@center",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Element.ALIGN_CENTER;
            }

            return Element.ALIGN_LEFT;
        }

        private static string StripHtmlTags(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return System.Text.RegularExpressions.Regex
                .Replace(value, "<.*?>", string.Empty)
                .Trim();
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (char invalidChar
                     in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName.Replace("/", "_");
        }
    }

    public sealed class PdfColumnDefinition
    {
        public string Name { get; set; }

        public string Caption { get; set; }

        public bool Hidden { get; set; }

        public int Width { get; set; }

        public string Align { get; set; }
    }
}
