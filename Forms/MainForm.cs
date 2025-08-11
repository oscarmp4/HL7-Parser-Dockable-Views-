using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using HL7ParserWin_ParseView.Services;
using NHapi.Base.Model;
using System.Linq;

namespace HL7ParserWin_ParseView.Forms
{
    public partial class MainForm : Form
    {
        private readonly Hl7ParserService _service = new Hl7ParserService();
        private IMessage _message;

        // Holds raw VMD text and parsed mappings
        private string _currentVmdText = string.Empty;

        // Simple “learned” mapping: FieldName -> TerserPath, e.g.  "MSHSendingApp" => "/MSH-3"
        private readonly System.Collections.Generic.Dictionary<string, string> _vmdMapping
            = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        // Track the raw line for each root segment node to support highlight-on-select
        private class SegmentNodeTag
        {
            public string SegmentId { get; set; }
            public int Occurrence { get; set; }   // 1-based order of this segment type
            public string RawLine { get; set; }   // The full raw line (e.g., "PID|1|...")
        }


        public MainForm()
        {
            InitializeComponent();            
        }

        private void btnParse_Click(object sender, EventArgs e)
        {
            try
            {
                var rawMessage = txtMessageRaw.Text;
                _message = _service.Parse(rawMessage);

                // ===== Tree View (unchanged) =====
                treeView.Nodes.Clear();
                var root = treeView.Nodes.Add("HL7 Message");
                foreach (var name in _message.Names)
                {
                    var structures = _message.GetAll(name);
                    for (int i = 0; i < structures.Length; i++)
                        root.Nodes.Add($"{name}[{i + 1}]");
                }
                root.Expand();
                                
                // ===== Build the formatted “Group RDE” block =====
                var sb = new StringBuilder();
                // Prefer MSH-8 for Group; if empty, fall back to MSH-9-1
                var groupRaw = GetMapped("GroupName", "/MSH-8");
                groupRaw = string.IsNullOrWhiteSpace(groupRaw) ? GetMapped("GroupFromMessageType", "/MSH-9-1") : groupRaw;

                // If MSH-8 already has quotes, strip them, then re-quote safely
                var groupClean = EscapeSingleQuotes(StripOuterQuotes(groupRaw));

                // Emit Group line exactly in single quotes
                sb.AppendLine($"Group '{groupClean}'");


                //sb.AppendLine("Group 'RDE'");
                //sb.AppendLine("Row 0");
                //sb.AppendLine("   Table Profile - ProfileInformationTable");
                //sb.AppendLine("   Row 0");

                // ---- MSH ----
                var mshSendingApp = GetMapped("MSHSendingApp", "/MSH-3");
                var mshSendingFacility = GetMapped("MSHSendingFacility", "/MSH-4");
                var mshReceivingApp = GetMapped("MSHReceivingApp", "/MSH-5");
                var mshReceivingFacility = GetMapped("MSHReceivingFacility", "/MSH-6");
                var mshControlId = GetMapped("MSHMessageControlID", "/MSH-10");
                var mshProcessingId = GetMapped("MSHProcessingID", "/MSH-11");
                var mshVersionId = GetMapped("MSHVersionID", "/MSH-12");
                var mshSequenceIdStr = Get("/MSH-13"); // Often blank; you want 0.000000
                double mshSequenceId = ParseDoubleOrDefault(mshSequenceIdStr, 0);

                // ---- PID ----
                // PID-5: XPN Family^Given
                var pidFamilyName = GetMapped("PIDFamilyName", "/PID-5-1");
                var pidGivenName = GetMapped("PIDGivenName", "/PID-5-2");
                var pidAccountNumber = GetMapped("PIDAccountNumber", "/PID-18");   // Account #
                                                                                   // PID-3: Medical Record Number. In sample it is "DCTIPU\\F\\1872" => "DCTIPU|1872"
                var pidMedRecRaw = GetMapped("PIDMedRec", "/PID-3");
                var pidMedRec = UnescapeHL7(pidMedRecRaw);

                // ---- PV1 ----
                // PV1-3 is PL: PointOfCare^Room^Bed^Facility...
                var pv1PatientClass = GetMapped("PV1PatientClass", "/PV1-2");
                var pv1PointOfCare = GetMapped("PV1PointofCare", "/PV1-3-1");
                var pv1Room = GetMapped("PV1Room", "/PV1-3-2");
                var pv1HospitalService = GetMapped("PV1HospitalService", "/PV1-10");   // may be blank in your sample
                var pv1AdmitDate = ParseHl7Ts(GetMapped("PV1AdmitDate", "/PV1-44"));   // 20230614000000
                var pv1DischargeDate = ParseHl7TsNullable(GetMapped("PV1DischargeDate", "/PV1-45"));

                // ---- ORC ----
                var orcOrderControl = GetMapped("ORCOrdercontrol", "/ORC-1");   // NW
                var orcPrescriptionNum = GetMapped("ORCPrescriptionNum", "/ORC-3"); // (empty in your sample)
                var orcStartDate = ParseHl7TsNullable(GetMapped("ORCStartDate", "/ORC-7")); // often null
                var orcStopDate = ParseHl7TsNullable(GetMapped("ORCStopDate", "/ORC-8"));  // often null
                var orcIntervalFreq = GetMapped("ORCIntervalFreq", "/ORC-4"); // blank in sample
                var orcIntervalTime = GetMapped("ORCIntervalTime", "/ORC-5"); // blank in sample
                var orcCondition = GetMapped("ORCCondition", "/ORC-6"); // blank in sample
                var orcTransactionDate = ParseHl7Ts(GetMapped("ORCTransactionDate", "/ORC-9"));// 20230615000000
                                                                                               // ORC-11: Verified By (XCN); take ID number (component 1)
                var orcVerifiedBy = GetMapped("ORCVerifiedBy", "/ORC-11-1");  // "1095"
                                                                              // ORC-12: Ordering Provider (XCN); take Given Name (component 3) or family/given as available
                var orcOrderingProvider = FirstNonEmpty(GetMapped("ORCOrderingProviderGiven", "/ORC-12-3"), GetMapped("ORCOrderingProviderFamily", "/ORC-12-2"), GetMapped("ORCOrderingProviderId", "/ORC-12-1"));

                // ---- RXE / RXO / RXR / TQ1 ----
                // RXE-3: Give Code (CWE/CE) -> ID and Text
                var rxeGiveCodeId = GetMapped("RXEGiveCodeID", "/RXE-3-1"); // "11917001338"
                var rxeGiveCodeText = GetMapped("RXEGiveCodeText", "/RXE-3-2"); // "NICOTINE     DIS 21MG/24H"
                                                                                // RXE-10 typically "Give Amount - Minimum" (your sample wants RXEQuantity=5.000000)
                var rxeQuantity = ParseDoubleOrDefault(GetMapped("RXEQuantity", "/RXE-10"), 0);
                // Optional min/max/units/dosage form – map to zero/empty if missing in your sample
                var rxeGiveMin = ParseDoubleOrDefault(GetMapped("RXEGiveMin", "/RXE-11"), 0);
                var rxeGiveMax = ParseDoubleOrDefault(GetMapped("RXEGiveMax", "/RXE-12"), 0);
                var rxeGiveUnit = GetMapped("RXEGiveUnit", "/RXE-13");// may be blank
                var rxeDosageForm = GetMapped("RXEDosageForm", "/RXE-2"); // sometimes CWE for dosage form, may be blank

                // Provider Admin Instructions: your sample shows "A1P TD QD" in TQ1-12-1; take that if present
                var rxeProvidersAdmin = FirstNonEmpty(GetMapped("RXEProvidersAdminInstructions", "/TQ1-12-1"), Get("/RXE-7-1"), Get("/RXE-7"));

                // RXR route – usually RXR-1-2 (text) or RXR-1-1 (ID); sample has blanks
                var rxrRoute = FirstNonEmpty(GetMapped("RXRRoute", "/RXR-1-2"), Get("/RXR-1-1"), Get("/RXR-1"));

                // ===== Emit to Parse View exactly as requested =====
                sb.AppendLine($"     MSHSendingApp = '{mshSendingApp}'");
                sb.AppendLine($"     MSHSendingFacility = '{mshSendingFacility}'");
                sb.AppendLine($"     MSHReceivingApp = '{mshReceivingApp}'");
                sb.AppendLine($"     MSHReceivingFacility = '{mshReceivingFacility}'");
                sb.AppendLine($"     MSHMessageControlID = '{mshControlId}'");
                sb.AppendLine($"     MSHProcessingID = '{mshProcessingId}'");
                sb.AppendLine($"     MSHVersionID = '{mshVersionId}'");
                sb.AppendLine($"     MSHSequenceID = {mshSequenceId:0.000000}");

                sb.AppendLine($"     PIDFamilyName = '{pidFamilyName}'");
                sb.AppendLine($"     PIDGivenName = '{pidGivenName}'");
                sb.AppendLine($"     PIDAccountNumber = '{pidAccountNumber}'");
                sb.AppendLine($"     PIDMedRec = '{pidMedRec}'");

                sb.AppendLine($"     PV1PointofCare = '{pv1PointOfCare}'");
                sb.AppendLine($"     PV1Room = '{pv1Room}'");
                sb.AppendLine($"     PV1DischargeDate = {FormatNullable(pv1DischargeDate)}");
                sb.AppendLine($"     PV1AdmitDate = {FormatNullable(pv1AdmitDate)}");
                sb.AppendLine($"     PV1HospitalService = '{pv1HospitalService}'");
                sb.AppendLine($"     PV1PatientClass = '{pv1PatientClass}'");

                sb.AppendLine($"     ORCOrdercontrol = '{orcOrderControl}'");
                sb.AppendLine($"     ORCPrescriptionNum = '{orcPrescriptionNum}'");
                sb.AppendLine($"     ORCStartDate = {FormatNullable(orcStartDate)}");
                sb.AppendLine($"     ORCStopDate = {FormatNullable(orcStopDate)}");
                sb.AppendLine($"     ORCIntervalFreq = '{orcIntervalFreq}'");
                sb.AppendLine($"     ORCIntervalTime = '{orcIntervalTime}'");
                sb.AppendLine($"     ORCCondition = '{orcCondition}'");
                sb.AppendLine($"     ORCTransactionDate = {FormatNullable(orcTransactionDate)}");
                sb.AppendLine($"     ORCVerifiedBy = '{orcVerifiedBy}'");
                sb.AppendLine($"     ORCOrderingProvider = '{orcOrderingProvider}'");

                sb.AppendLine($"     RXEQuantity = {rxeQuantity:0.000000}");
                sb.AppendLine($"     RXEGiveCodeText = '{rxeGiveCodeText}'");
                sb.AppendLine($"     RXEGiveCodeID = '{rxeGiveCodeId}'");
                sb.AppendLine($"     RXEGiveMin = {rxeGiveMin:0.000000}");
                sb.AppendLine($"     RXEGiveMax = {rxeGiveMax:0.000000}");
                sb.AppendLine($"     RXEGiveUnit = '{rxeGiveUnit}'");
                sb.AppendLine($"     RXEDosageForm = '{rxeDosageForm}'");
                sb.AppendLine($"     RXEProvidersAdminInstructions = '{rxeProvidersAdmin}'");
                sb.AppendLine($"     RXRRoute = '{rxrRoute}'");

                sb.AppendLine("   Table Allergy - Repeating Allergies");
                sb.AppendLine("   Table ProfileRXC - Repeating RXC");
                sb.AppendLine("   Table Notes - Repeating Notes");
                sb.AppendLine("   Row 0");
                sb.AppendLine("     Note = ' '");

                // Push to editable Parse View
                txtParseView.Text = sb.ToString();

                // Keep raw message visible
                txtMessageRaw.Text = rawMessage;                                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            BuildTreeFromMessage(txtMessageRaw.Text);
        }


        // Add this Tree builder + helpers
        private void BuildTreeFromMessage(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                treeView.Nodes.Clear();
                return;
            }

            // Normalize line breaks and split into segment lines
            var text = rawMessage.Replace("\r\n", "\r").Replace("\n", "\r");
            var lines = text.Split(new[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Count occurrences per segment to label e.g., PID[2]
            var segCounters = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            treeView.BeginUpdate();
            try
            {
                treeView.Nodes.Clear();
                var root = treeView.Nodes.Add("HL7 Message");

                foreach (var line in lines)
                {
                    if (line.Length < 4 || line[3] != '|') continue;  // skip bad lines
                    var segId = line.Substring(0, 3);

                    if (!segCounters.ContainsKey(segId)) segCounters[segId] = 0;
                    segCounters[segId]++;

                    // Build preview for the node text (show some important fields inline)
                    string nodeText = $"{segId}[{segCounters[segId]}] — {BuildSegmentPreview(segId, line)}";
                    var segNode = root.Nodes.Add(nodeText);

                    // Tag for highlight-on-select
                    segNode.Tag = new SegmentNodeTag
                    {
                        SegmentId = segId,
                        Occurrence = segCounters[segId],
                        RawLine = line
                    };

                    // Add field/component breakdown
                    PopulateFieldNodes(segNode, segId, line);
                }

                root.Expand();
            }
            finally
            {
                treeView.EndUpdate();
            }

            // Hook selection for highlight (only once)
            treeView.AfterSelect -= treeView_AfterSelect_Highlight;
            treeView.AfterSelect += treeView_AfterSelect_Highlight;
        }

        private static string BuildSegmentPreview(string segId, string rawLine)
        {
            // Generic preview: show first few fields joined by " | "
            // Special-case some common segments for nicer previews
            var fields = rawLine.Split('|');
            // fields[0] = SEG
            string safe(int idx) => (idx >= 1 && idx < fields.Length) ? fields[idx] : string.Empty;

            switch (segId)
            {
                case "MSH":
                    // MSH-9 (MsgType), MSH-10 (Ctrl ID), MSH-11 (Proc ID), MSH-12 (Version)
                    return $"{safe(8)} | {safe(9)} | {safe(10)} | {safe(11)}";
                case "PID":
                    // PID-3 (MRN), PID-5 (Name), PID-18 (Account)
                    return $"{safe(2)} | {safe(4)} | {safe(17)}";
                case "PV1":
                    // PV1-2 (Class), PV1-3 (Location), PV1-44 (Admit)
                    return $"{safe(1)} | {safe(2)} | {safe(43)}";
                case "ORC":
                    // ORC-1 (Ctrl), ORC-9 (Date/Time Tx), ORC-12 (Ordering Prov)
                    return $"{safe(1)} | {safe(8)} | {safe(11)}";
                case "RXE":
                    // RXE-3 (Give Code), RXE-10 (Qty), TQ1 often carries sig but show RXE-7 if present
                    return $"{safe(2)} | Qty={safe(9)} | {safe(6)}";
                case "RXO":
                    // RXO-2 (Requested Code)
                    return $"{safe(1)}";
                default:
                    // Fallback: first 3 fields
                    return string.Join(" | ", System.Linq.Enumerable.Range(1, System.Math.Min(3, fields.Length - 1)).Select(i => fields[i]));
            }
        }

        private void PopulateFieldNodes(TreeNode segNode, string segId, string rawLine)
        {
            var fields = rawLine.Split('|');
            for (int f = 1; f < fields.Length; f++)
            {
                string fieldVal = fields[f];
                var fieldNode = segNode.Nodes.Add($"{segId}-{f}: {fieldVal}");

                // Components: ^
                var comps = fieldVal.Split('^');
                if (comps.Length > 1)
                {
                    for (int c = 0; c < comps.Length; c++)
                    {
                        var compVal = comps[c];
                        var compNode = fieldNode.Nodes.Add($"{segId}-{f}-{c + 1}: {compVal}");

                        // Subcomponents: &
                        var subs = compVal.Split('&');
                        if (subs.Length > 1)
                        {
                            for (int s = 0; s < subs.Length; s++)
                            {
                                compNode.Nodes.Add($"{segId}-{f}-{c + 1}-{s + 1}: {subs[s]}");
                            }
                        }
                    }
                }
            }
        }
        //Add the TreeView highlight handler
        private void treeView_AfterSelect_Highlight(object sender, TreeViewEventArgs e)
        {
            var tag = e.Node?.Tag as SegmentNodeTag;
            if (tag == null || string.IsNullOrEmpty(tag.RawLine)) return;

            // Find the Nth occurrence of that exact raw line in the Message View and select it
            string text = txtMessageRaw.Text.Replace("\r\n", "\r").Replace("\n", "\r");
            int startIndex = FindNthOccurrence(text, tag.RawLine, tag.Occurrence);
            if (startIndex >= 0)
            {
                txtMessageRaw.SelectionStart = startIndex;
                txtMessageRaw.SelectionLength = tag.RawLine.Length;
                txtMessageRaw.ScrollToCaret();
                txtMessageRaw.Focus();
            }
        }

        private static int FindNthOccurrence(string haystack, string needle, int n)
        {
            if (string.IsNullOrEmpty(needle) || n <= 0) return -1;
            int pos = -1, from = 0;
            for (int i = 0; i < n; i++)
            {
                pos = haystack.IndexOf(needle, from, StringComparison.Ordinal);
                if (pos < 0) return -1;
                from = pos + needle.Length;
            }
            return pos;
        }





        /// <summary>
        /// Gets a field value by friendly name using learned VMD mapping if available,
        /// otherwise uses the provided fallback Terser path.
        /// Example: GetMapped("MSHSendingApp", "/MSH-3")
        /// </summary>
        private string GetMapped(string friendlyKey, string fallbackTerser)
        {
            if (!string.IsNullOrEmpty(friendlyKey) && _vmdMapping.TryGetValue(friendlyKey, out var mappedPath))
            {
                var v = _service.GetField(_message, mappedPath);
                if (!string.IsNullOrEmpty(v)) return v;
                // If mapped path returns empty, fall back
            }
            return _service.GetField(_message, fallbackTerser) ?? string.Empty;
        }


        private string Get(string terserPath)
        {
            // Thin wrapper around your service; normalizes null → empty
            var v = _service.GetField(_message, terserPath);
            return v ?? string.Empty;
        }

        private static double ParseDoubleOrDefault(string s, double defVal)
        {
            if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var d))
                return d;
            return defVal;
        }

        private static string UnescapeHL7(string value)
        {
            // Basic HL7 escape sequences
            if (string.IsNullOrEmpty(value)) return value;
            return value
                .Replace(@"\F\", "|")
                .Replace(@"\S\", "^")
                .Replace(@"\T\", "&")
                .Replace(@"\R\", "~")
                .Replace(@"\E\", @"\");
        }

        private static DateTime? ParseHl7TsNullable(string ts)
        {
            var result = ParseHl7Ts(ts);
            return result == null ? (DateTime?)null : result.Value;
        }

        private static DateTime? ParseHl7Ts(string ts)
        {
            // Supports YYYYMMDD or YYYYMMDDHHMMSS (ignores timezone for now)
            if (string.IsNullOrWhiteSpace(ts)) return null;
            ts = ts.Trim();
            string[] formats = { "yyyyMMddHHmmss", "yyyyMMdd" };
            if (DateTime.TryParseExact(ts, formats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt))
                return dt;
            return null;
        }

        private static string FormatNullable(DateTime? dt)
        {
            // Matches your desired "2023/06/14 00:00:00" or "<null>"
            return dt.HasValue
                ? dt.Value.ToString("yyyy/MM/dd HH:mm:ss")
                : "<null>";
        }

        private static string FirstNonEmpty(params string[] vals)
        {
            foreach (var v in vals)
                if (!string.IsNullOrWhiteSpace(v)) return v;
            return string.Empty;
        }


        private void btnLoadSample_Click(object sender, EventArgs e)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleMessages", "sample.hl7");
            if (File.Exists(path))
                txtMessageRaw.Text = File.ReadAllText(path);
        }

        private void txtParseView_MouseClick(object sender, MouseEventArgs e)
        {
            // Find which line in Parse View was clicked
            int index = txtParseView.GetCharIndexFromPosition(e.Location);
            string[] lines = txtParseView.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            int clickedLine = txtParseView.GetLineFromCharIndex(index);
            if (clickedLine >= 0 && clickedLine < lines.Length)
            {
                string line = lines[clickedLine];
                string searchValue = ExtractKeyword(line);

                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    int pos = txtMessageRaw.Text.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase);
                    if (pos >= 0)
                    {
                        txtMessageRaw.SelectionStart = pos;
                        txtMessageRaw.SelectionLength = searchValue.Length;
                        txtMessageRaw.ScrollToCaret();
                        txtMessageRaw.Focus();
                    }
                }
            }
        }

        private string ExtractKeyword(string line)
        {
            // Priority 1: extract value after the colon (field value)
            if (line.Contains(":"))
            {
                string valuePart = line.Substring(line.IndexOf(":") + 1).Trim();
                if (!string.IsNullOrEmpty(valuePart))
                    return valuePart;
            }

            // Priority 2: match by known HL7 segment keyword
            if (line.Contains("RXO")) return "RXO";
            if (line.Contains("RXE")) return "RXE";
            if (line.Contains("PID")) return "PID";
            if (line.Contains("PV1")) return "PV1";
            if (line.Contains("ORC")) return "ORC";
            if (line.Contains("MSH")) return "MSH";

            // Fallback: return whole trimmed line
            return line.Trim();
        }

        private void btnPasteClipboard_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                txtMessageRaw.Text = Clipboard.GetText();
            }
            else
            {
                MessageBox.Show("Clipboard does not contain text.", "Paste Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnGetVMD_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Open Visual Message Definition (.vmd)";
                ofd.Filter = "Visual Message Definition (*.vmd)|*.vmd|All files (*.*)|*.*";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        _currentVmdText = System.IO.File.ReadAllText(ofd.FileName);
                        txtVmdView.Text = _currentVmdText;

                        // Try to learn mappings from the file
                        LearnVmdMappings(_currentVmdText);

                        MessageBox.Show(this,
                            _vmdMapping.Count > 0
                                ? $"VMD loaded. Learned {_vmdMapping.Count} mapping(s)."
                                : "VMD loaded. No explicit mappings found; default paths will be used.",
                            "Get VMD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Failed to load VMD: {ex.Message}", "Get VMD",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtMessageRaw.Clear();
            txtParseView.Clear();
            treeView.Nodes.Clear();
        }

        /// <summary>
        /// Tries to learn field mappings from the VMD file content.
        /// Preferred format inside the VMD (anywhere):
        /// [Mapping]
        /// MSHSendingApp = /MSH-3
        /// PIDFamilyName = /PID-5-1
        /// ...
        /// If no [Mapping] block is found, will still scan lines like `Key = /Segment-Field-Component`.
        /// </summary>
        private void LearnVmdMappings(string vmdText)
        {
            _vmdMapping.Clear();
            if (string.IsNullOrWhiteSpace(vmdText)) return;

            var lines = vmdText.Replace("\r\n", "\n").Split('\n');

            bool inMapping = false;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    inMapping = string.Equals(line, "[Mapping]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                // Parse lines of the form "Key = Value"
                int eq = line.IndexOf('=');
                if (eq > 0)
                {
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();

                    // Accept when we're in [Mapping] or when the RHS looks like a Terser path
                    if (inMapping || LooksLikeTerser(val))
                    {
                        // Normalize: allow quotes
                        if (val.Length >= 2 && (val.StartsWith("'") && val.EndsWith("'") || val.StartsWith("\"") && val.EndsWith("\"")))
                            val = val.Substring(1, val.Length - 2);

                        if (!_vmdMapping.ContainsKey(key))
                            _vmdMapping.Add(key, val);
                        else
                            _vmdMapping[key] = val;
                    }
                }
            }
        }

        private static bool LooksLikeTerser(string val)
        {
            // Very light heuristic: starts with slash, contains '-', e.g. /MSH-3, /PID-5-1
            if (string.IsNullOrEmpty(val)) return false;
            val = val.Trim();
            return val.StartsWith("/") && val.IndexOf('-', 1) > 1;
        }

        private static string StripOuterQuotes(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            s = s.Trim();
            if ((s.StartsWith("'") && s.EndsWith("'")) || (s.StartsWith("\"") && s.EndsWith("\"")))
                s = s.Substring(1, s.Length - 2);
            return s;
        }

        private static string EscapeSingleQuotes(string s)
        {
            return string.IsNullOrEmpty(s) ? string.Empty : s.Replace("'", "''");
        }


    }
}
