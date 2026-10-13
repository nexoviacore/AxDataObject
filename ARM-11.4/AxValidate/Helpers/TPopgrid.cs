namespace AxValidate.Helpers;

[Serializable]
public class TPopgrid
{
    public int FrameNo { get; set; } = 0;
    public int ParentFrameNo { get; set; } = 0;
    public int PopAt { get; set; } = 0;
    public string PopName { get; set; } = string.Empty;
    public string Heading { get; set; } = string.Empty;
    public string Parent { get; set; } = string.Empty;
    public string ParentField { get; set; } = string.Empty;
    public string PopCond { get; set; } = string.Empty;
    public string ShowButtons { get; set; } = string.Empty;
    public string INode { get; set; } = string.Empty;
    public string KeyCols { get; set; } = string.Empty;
    public string DispField { get; set; } = string.Empty;
    public string SumFormat { get; set; } = string.Empty;
    public string Delimiter { get; set; } = string.Empty;
    public bool AutoShow { get; set; } = false;
    public bool FirmBind { get; set; } = false;
    public bool AutoFillFld { get; set; } = false;
    public List<string> AutoFill { get; set; } = new List<string>();
}