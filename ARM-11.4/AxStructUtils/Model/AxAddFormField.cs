namespace AxStructUtils
{
    public class AxAddFormField
    {
        public string ARMSessionId { get; set; }
        public string? mode { get; set; }
        public string? projName { get; set; }
        public string? userName { get; set; }
        public bool trace { get; set; }
        public string structName { get; set; }
        public string? structCaption { get; set; }
        public string structType { get; set; }
        public bool isComp { get; set; }
        public string compJSON { get; set; } = "{}";
        public string? propvalJSON { get; set; }
        public string? fieldJSON { get; set; }
        public string runtimeInfo { get; set; } = "{}";
    }

    public class AxAddFieldApiResponse<T>
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;  // Default message as empty string
        public T data { get; set; } = default!;  // Default data as an empty object (null for reference types)

        // Optional constructor for easier initialization
        public AxAddFieldApiResponse(bool success = false, string message = "", T data = default!)
        {
            this.success = success;
            this.message = message;
            this.data = data;
        }
    }
}
