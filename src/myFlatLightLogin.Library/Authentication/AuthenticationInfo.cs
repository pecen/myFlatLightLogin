using Csla;

namespace myFlatLightLogin.Library.Authentication
{
    [Serializable]
    public class AuthenticationInfo : ReadOnlyBase<AuthenticationInfo>
    {
        #region Properties

        public static readonly PropertyInfo<int> IdProperty = RegisterProperty<int>(c => c.Id);
        public int Id
        {
            get { return GetProperty(IdProperty); }
            set { LoadProperty(IdProperty, value); }
        }

        public static readonly PropertyInfo<string> MakeProperty = RegisterProperty<string>(c => c.Make);
        public string Make
        {
            get { return GetProperty(MakeProperty); }
            set { LoadProperty(MakeProperty, value); }
        }

        public static readonly PropertyInfo<string> ModelProperty = RegisterProperty<string>(c => c.Model);
        public string Model
        {
            get { return GetProperty(ModelProperty); }
            set { LoadProperty(ModelProperty, value); }
        }

        public string FullName
        {
            get { return $"{Make} {Model}"; }
        }

        public static readonly PropertyInfo<string> LicensePlateProperty = RegisterProperty<string>(c => c.LicensePlate);
        public string LicensePlate
        {
            get { return GetProperty(LicensePlateProperty); }
            //get => !string.IsNullOrEmpty(Note) ? $"{GetProperty(LicensePlateProperty)} | {GetProperty(NoteProperty)}" : GetProperty(LicensePlateProperty);

            set { LoadProperty(LicensePlateProperty, value); }
        }

        public static readonly PropertyInfo<string> NoteProperty = RegisterProperty<string>(c => c.Note);

        public string Note
        {
            get { return GetProperty(NoteProperty); }
            set { LoadProperty(NoteProperty, value); }
        }

        //public static readonly PropertyInfo<DistanceInfo> DistanceUnitProperty = RegisterProperty<DistanceInfo>(c => c.DistanceUnit);
        //public DistanceInfo DistanceUnit {
        //  get { return GetProperty(DistanceUnitProperty); }
        //  set { LoadProperty(DistanceUnitProperty, value); }
        //}

        public static readonly PropertyInfo<DistanceUnits> DistanceUnitProperty = RegisterProperty<DistanceUnits>(c => c.DistanceUnit);
        public DistanceUnits DistanceUnit
        {
            get { return GetProperty(DistanceUnitProperty); }
            set { LoadProperty(DistanceUnitProperty, value); }
        }

        //public static readonly PropertyInfo<VolumeInfo> VolumeProperty = RegisterProperty<VolumeInfo>(c => c.VolumeUnit);
        //public VolumeInfo VolumeUnit {
        //  get { return GetProperty(VolumeProperty); }
        //  set { LoadProperty(VolumeProperty, value); }
        //}

        public static readonly PropertyInfo<VolumeUnits> VolumeProperty = RegisterProperty<VolumeUnits>(c => c.VolumeUnit);
        public VolumeUnits VolumeUnit
        {
            get { return GetProperty(VolumeProperty); }
            set { LoadProperty(VolumeProperty, value); }
        }

        //public static readonly PropertyInfo<ConsumptionUnitType> ConsumptionUnitProperty = RegisterProperty<ConsumptionUnitType>(c => c.ConsumptionUnit);
        //public ConsumptionUnitType ConsumptionUnit {
        //  get { return GetProperty(ConsumptionUnitProperty); }
        //  set { LoadProperty(ConsumptionUnitProperty, value); }
        //}

        public static readonly PropertyInfo<ConsumptionUnits> ConsumptionUnitProperty = RegisterProperty<ConsumptionUnits>(c => c.ConsumptionUnit);
        public ConsumptionUnits ConsumptionUnit
        {
            get { return GetProperty(ConsumptionUnitProperty); }
            set { LoadProperty(ConsumptionUnitProperty, value); }
        }

        // To be implemented
        //
        //public string ChosenUnits {
        //  get { return DistanceUnit.ShortName + ", " + VolumeUnit.ShortName + ", " + ConsumptionUnit.Name; }
        //  //get { return DistanceUnit.ShortName + ", " + VolumeUnit.ShortName + ", " + ConsumptionUnit.GetEnumDescription(); }
        //}

        public static readonly PropertyInfo<string> TotalFillupsProperty = RegisterProperty<string>(c => c.TotalFillups);
        public string TotalFillups
        {
            get { return GetProperty(TotalFillupsProperty); }
            set { LoadProperty(TotalFillupsProperty, value); }
        }

        public static readonly PropertyInfo<string> TotalDistanceProperty = RegisterProperty<string>(c => c.TotalDistance);
        public string TotalDistance
        {
            get { return GetProperty(TotalDistanceProperty); }
            set { LoadProperty(TotalDistanceProperty, value); }
        }

        public static readonly PropertyInfo<string> AverageConsumptionProperty = RegisterProperty<string>(c => c.AverageConsumption);
        public string AverageConsumption
        {
            get { return GetProperty(AverageConsumptionProperty); }
            set { LoadProperty(AverageConsumptionProperty, value); }
        }

        public static readonly PropertyInfo<DateTime> DateAddedProperty = RegisterProperty<DateTime>(c => c.DateAdded);
        public DateTime DateAdded
        {
            get { return GetProperty(DateAddedProperty); }
            set { LoadProperty(DateAddedProperty, value); }
        }

        public static readonly PropertyInfo<DateTime> LastModifiedProperty = RegisterProperty<DateTime>(c => c.LastModified);
        public DateTime LastModified
        {
            get { return GetProperty(LastModifiedProperty); }
            set { LoadProperty(LastModifiedProperty, value); }
        }

        public static readonly PropertyInfo<FillupList> FillupsProperty = RegisterProperty<FillupList>(c => c.Fillups);
        public FillupList Fillups
        {
            get { return GetProperty(FillupsProperty); }
            set { LoadProperty(FillupsProperty, value); }
        }

        //public static readonly PropertyInfo<CarSettingsInfo> CarSettingsProperty = RegisterProperty<CarSettingsInfo>(c => c.CarSettings);
        //public CarSettingsInfo CarSettings
        //{
        //    get { return GetProperty(CarSettingsProperty); }
        //    set { LoadProperty(CarSettingsProperty, value); }
        //}

        //public static readonly PropertyInfo<CarStatisticsInfo> CarStatisticsProperty = RegisterProperty<CarStatisticsInfo>(c => c.CarStatistics);
        //public CarStatisticsInfo CarStatistics
        //{
        //    get { return GetProperty(CarStatisticsProperty); }
        //    set { LoadProperty(CarStatisticsProperty, value); }
        //}

        #endregion
    }
}
