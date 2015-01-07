
// TODO: attributes, lastwrite/read/access, react on non connected database
namespace MSSQLFS
{
   

    class Program
    {

        static void Main(string[] args)
        {
            Dokan.DokanOptions opt = new Dokan.DokanOptions();
            opt.DriveLetter = 'r';
            opt.NetworkDrive = true;
            opt.DebugMode = false;
            opt.UseAltStream = false;
            opt.UseKeepAlive = true;
            opt.UseStdErr = true;
            opt.VolumeLabel = "MSSQLFS";

            string Server = "", Database = "", User = "", Password = "";


            opt.DriveLetter = (System.Configuration.ConfigurationManager.AppSettings["drive"] != null) ? System.Configuration.ConfigurationManager.AppSettings["drive"][0] : 'r';
            Server = System.Configuration.ConfigurationManager.AppSettings["server"];
            Database = System.Configuration.ConfigurationManager.AppSettings["database"];
            User = System.Configuration.ConfigurationManager.AppSettings["user"];
            Password = System.Configuration.ConfigurationManager.AppSettings["password"];


            foreach (string arg in args)
            {
                if (System.Text.RegularExpressions.Regex.Match(arg, "/drive:.").Success)
                    opt.DriveLetter = System.Text.RegularExpressions.Regex.Split(arg, "/drive:")[1][0];
                if (System.Text.RegularExpressions.Regex.Match(arg, "/server:*").Success)
                    Server = System.Text.RegularExpressions.Regex.Split(arg, "/server:")[1];
                if (System.Text.RegularExpressions.Regex.Match(arg, "/database:*").Success)
                    Database = System.Text.RegularExpressions.Regex.Split(arg, "/database:")[1];
                if (System.Text.RegularExpressions.Regex.Match(arg, "/user:*").Success)
                    User = System.Text.RegularExpressions.Regex.Split(arg, "/user:")[1];
                if (System.Text.RegularExpressions.Regex.Match(arg, "/password:*").Success)
                    Password = System.Text.RegularExpressions.Regex.Split(arg, "/password:")[1];
            }

            string ConnString = string.Format("Data Source={0};Initial Catalog={1};Integrated Security=False;User ID={2};Password={3};Pooling=true;Min Pool Size=1;Max Pool Size=5;Connect Timeout=500", Server, Database, User, Password);
            Dokan.DokanNet.DokanMain(opt, new MSSQLFS(ConnString));
        }


    }


}

/*

     System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["oldPlace"].Value = "3";     
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
*/ 
