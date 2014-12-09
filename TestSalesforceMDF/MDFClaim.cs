using System;
using System.Net.Http;

namespace TestSalesforceMDF
{
    public class MDFClaimForm
    {
        public string MDFRequestNumber { get; set; }
        public string MDFRequestNumberError { get; set; }
        public string PartnerName { get; set; }
        public string PartnerNameError { get; set; }
        public string SubmittersName { get; set; }
        public string SubmittersNameError { get; set; }
        public string PartnerId { get; set; }
        public string PartnerIdError { get; set; }
        public string NameOfActivity { get; set; }
        public string NameOfActivityError { get; set; }
        public string ActivityEndDate { get; set; }
        public string ActivityEndDateError { get; set; }
        public string ActivityStartDate { get; set; }
        public string ActivityStartDateError { get; set; }
        public string ActualClaimAmount { get; set; }
        public string ActualClaimAmountError { get; set; }
        public string ShortTermMetricsError { get; set; }
        public string DirectMarketingMetric { get; set; }
        public string EventsMetric { get; set; }
        public string PartnerMetric { get; set; }
        public string OtherMetric { get; set; }
        public string ShortDescriptionOutcome { get; set; }
        public string ShortDescriptionOutcomeError { get; set; }
        public string ProofFileError { get; set; }
        public string SubmitError { get; set; }

        public bool FillErrorMessages()
        {
            var hasError = false;
            foreach (var prop in typeof(MDFClaimForm).GetProperties())
            {
                var name = prop.Name;
                if (prop.CanRead && prop.PropertyType == typeof(string) && !name.Contains("Error") &&
                    !name.Contains("Metric"))
                {
                    var message = string.Empty;
                    var value = (string)prop.GetValue(this, null);
                    if (String.IsNullOrWhiteSpace(value))
                    {
                        message = "is a required field.";
                        hasError = true;
                    }
                    var errorPropInfo = GetType().GetProperty(string.Format("{0}Error", name));
                    if (errorPropInfo != null) errorPropInfo.SetValue(this, message);
                }
            }
            if (String.IsNullOrWhiteSpace(DirectMarketingMetric) && String.IsNullOrWhiteSpace(EventsMetric)
                && String.IsNullOrWhiteSpace(PartnerMetric) && String.IsNullOrWhiteSpace(OtherMetric))
            {
                ShortTermMetricsError = "This field is required.";
                hasError = true;
            }
            else
            {
                ShortTermMetricsError = string.Empty;
            }
            if (hasError) SubmitError = "You must complete all fields.";
            return hasError;
        }

        public MultipartFormDataContent GetMultipartContent()
        {
            var multipartContent = new MultipartFormDataContent();
            foreach (var prop in typeof(MDFClaimForm).GetProperties())
            {
                var name = prop.Name;
                if (prop.CanRead && prop.PropertyType == typeof(string) && !name.Contains("Error"))
                {
                    multipartContent.Add(new StringContent((string)prop.GetValue(this, null)), name);
                }
            }
            return multipartContent;
        }
    }

    public class ProofFile
    {
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public byte[] FileData { get; set; }
    }
}
