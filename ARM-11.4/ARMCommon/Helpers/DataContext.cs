
using ARMCommon.Model;
using Microsoft.EntityFrameworkCore;


namespace ARMCommon.Helpers
{
    public class DataContext : DbContext
    {
        protected readonly IConfiguration Configuration;

        public DataContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseNpgsql(Configuration.GetConnectionString("WebApiDatabase"));
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ARMUser>().ToTable("ARMUsers");
        }

        public DbSet<AxPage> axPages { get; set; }

        public DbSet<AxHomePageCard> axHomePageCards { get; set; }
        public DbSet<ARMServiceLogs> ARMServiceLogs { get; set; }
        public DbSet<ARMUser> ARMUsers { get; set; }
        public DbSet<ARMNotificationTemplate>? NotificationTemplate { get; set; }
        public DbSet<ARMUserGroup> ARMUserGroups { get; set; }
        public DbSet<UserModel>? ARMSignInUsers { get; set; }
        public DbSet<ARMDataSource>? ARMDataSources { get; set; }
        public DbSet<SQLDataSource> SQLDataSource { get; set; }
        public DbSet<ARMHtml>? ARMDefinations { get; set; }
        public DbSet<APIDefinitions>? APIDefinitions { get; set; }
        public DbSet<ARMApp> ARMApps { get; set; }
        public DbSet<AxpertUsers> AxpertUsers { get; set; }
        public DbSet<AxInlineForm> AxInLineForm { get; set; }
        public DbSet<AxModulePages> AxModulePages { get; set; }
        public DbSet<AxModule> AxModules { get; set; }
        public DbSet<AxSubModule> AxSubModules { get; set; }
        public DbSet<UserDevice> ARMUserDevices { get; set; }
        public DbSet<ARMPageData> PatientRegistration{ get; set; }
        public DbSet<ARMLogs> armlogs { get; set; }


    }
}

