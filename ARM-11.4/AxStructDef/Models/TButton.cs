
namespace AxStructDef.Models
{
    public class TButton
    {
        public string Parent { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string SName { get; set; } = string.Empty;
        public string Tlhw { get; set; } = string.Empty;
        public bool Flat { get; set; } = false;
        public string Image { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Container { get; set; } = string.Empty;
        public string Hint { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Task { get; set; } = string.Empty;
        public string Scut { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string Font { get; set; } = string.Empty;
        public bool Footer { get; set; } = false;
        public bool Visible { get; set; } = false;
        public string Api { get; set; } = string.Empty;
        public ButtonIcon Icon { get; set; } = new ButtonIcon();
        public List<ButtonPop> Pops { get; set; } = new List<ButtonPop>();
    }

    public class ButtonIcon
    {
        public string Text { get; set; } = string.Empty;
        public string AddClass { get; set; } = string.Empty;
    }

    public class ButtonPop
    {
        public string Caption { get; set; } = string.Empty;
        public string Task { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

}
