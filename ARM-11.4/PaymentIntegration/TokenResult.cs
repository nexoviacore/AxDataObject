namespace Program
{
    public class AxRequest
    {
        public string? REQUESTID { get; set; }
        public DateTime? REQUESTRECEIVEDTIME { get; set; }
        public string? SOURCEFROM { get; set; }
        public string? REQUESTSTRING { get; set; }
        public string? HEADERS { get; set; }
        public string? PARAMS { get; set; }
        public string? AUTHZ { get; set; }
        public string? CONTENTTYPE { get; set; }
        public string? CONTENTLENGTH { get; set; }
        public string? HOST { get; set; }
        public string? URL { get; set; }
        public string? ENDPOINT { get; set; }
        public string? REQUESTMETHOD { get; set; }
        public string? USERNAME { get; set; }
        public string? ADDITIONALDETAILS { get; set; }
        public string? SOURCEMACHINEIP { get; set; }
        public string? APINAME { get; set; }
        public string? RESPONSEID { get; set; }
        public DateTime? RESPONSESENTTIME { get; set; }
        public string? RESPONSESSTRING { get; set; }
        public int STATUSCODE { get; set; }
        public string? EXECUTIONTIME { get; set; }
        public string? ERRORDDETAILS { get; set; }
    }

    public class TokenResult
    {
        public string TOKEN { get; set; }
        public AxRequest REQUESTDATA { get; set; }
        public AxRequest PaymentRequestData { get; set; }
        public AxRequest PAYMENTPROCESSDATA { get; set; }
    }
}
