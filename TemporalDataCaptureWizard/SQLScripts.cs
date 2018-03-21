using System.Collections.Specialized;

namespace TemporalDataCaptureWizard
{

    class SQLScripts
    {
        private StringCollection scripts;
        public  StringCollection Scripts
        {
            get
            {
                return scripts;
            }

            set
            {
                scripts = value;
            }
        }
        public SQLScripts()
        {
            Scripts = new StringCollection();
            Scripts.Add(@".\SQL Scripts\01 dbo.CleanUpTDCObjects.sql");
            Scripts.Add(@".\SQL Scripts\02 dbo.usp_tdc_create_schema.sql");
            Scripts.Add(@".\SQL Scripts\03 dbo.ufn_tdc_objects_exists.sql");
            Scripts.Add(@".\SQL Scripts\04 dbo.ufn_is_tdc_enabled.sql");
            Scripts.Add(@".\SQL Scripts\05 dbo.usp_tdc_enable_db.sql");
            Scripts.Add(@".\SQL Scripts\06 dbo execute usp_tdc_enable_db.sql");
            Scripts.Add(@".\SQL Scripts\07 dbo.usp_tdc_disable_db.sql");
            Scripts.Add(@".\SQL Scripts\08 tdc.usp_GetColumnInfo.sql");
            Scripts.Add(@".\SQL Scripts\09 tdc.usp_GetJoinInfo.sql");
            Scripts.Add(@".\SQL Scripts\10 tdc.usp_tdc_enable_table_internal.sql");
            Scripts.Add(@".\SQL Scripts\11 tdc.usp_tdc_disable_table.sql");
            Scripts.Add(@".\SQL Scripts\12 tdc.usp_tdc_enable_table.sql");
            Scripts.Add(@".\SQL Scripts\13 tdc.ufn_ColumnType.sql");
            Scripts.Add(@".\SQL Scripts\14 tdc.usp_CanTableBeTDC_Enabled.sql");
        }


    }
}
