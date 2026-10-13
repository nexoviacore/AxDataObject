using System.ComponentModel.DataAnnotations;

namespace ARMCommon.Model
{
    public class AxPage
    {
        [Key]
        public string Name { get; set; }
        public string Menupath { get; set; }
        
        public string Caption { get; set; }
        public string Icon { get; set; }
        public string Pagetype { get; set; }
        public string Img { get; set; }
        public string Type { get; set; }
        public string Parent { get; set; }
        public string Visible { get; set; }
        public int Levelno { get; set; }

        public int Ordno { get; set; }
        public string Updatedby { get; set; }
        public DateTime Createdon { get; set; }
        public DateTime Importedon { get; set; }
        public string Createdby { get; set; }
        public string Category { get; set; }
    }

}
