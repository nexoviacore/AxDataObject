namespace ARM_APIs.Interface
{
    public interface ISendEmailService
    {
        Task<string> SendEmailJob(string senderEmail, string username,string JobId, int intervalSeconds,bool DeletePendingJobs);

        Task<string> DeleteExistingJobs(string jobId);
    }
}
