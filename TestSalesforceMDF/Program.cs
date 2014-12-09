using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;


namespace TestSalesforceMDF
{
    class Program
    {
        static void Main(string[] args)
        {
            var claimForm = new MDFClaimForm
            {
                ActivityEndDate = "2014-10-10T00:00:00Z",
                ActivityStartDate = "2014-10-11T00:00:00Z",
                NameOfActivity = "Console App Test",
                ActualClaimAmount = "100",
                DirectMarketingMetric = "console app test",
                MDFRequestNumber = "CRBMDF000126",
                PartnerName = "console app test",
                PartnerId = "console app test",
                SubmittersName = "console app test",
                ShortDescriptionOutcome = "console app test"
            };
            var file = GetUploadedFile();
            SendToSalesforce(claimForm, file);
            Console.Read();

        }

        private static ProofFile GetUploadedFile()
        {
            var fileName = "Untitled.png";
            var fileContentType = "image/png";
            var fileBytes = File.ReadAllBytes(@"C:\Users\emierd\Pictures\Untitled.png");

            var mimeType = FileUploadSecurityHelper.GetRealMimeFromFile(fileBytes, fileName, fileContentType);
            var proofFile = new ProofFile
            {
                FileData = fileBytes,
                FileName = fileName,
                MimeType = mimeType
            };
            return proofFile;
        }

        private static bool SendToSalesforce(MDFClaimForm claimForm, ProofFile file)
        {
            var sfBaseUrl = "http://localhost:22308";
            var sfApiRequest = "api/mdfclaim-form";
            int timeout = 60000;
            try
            {
                var httpClient = new HttpClient {BaseAddress = new Uri(sfBaseUrl)};
                var fileContent = new StreamContent(new MemoryStream(file.FileData));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.MimeType);
                var request = WebRequest.Create(sfBaseUrl);
                request.Method = "POST";
                request.Timeout = timeout;
                var formData = new MultipartFormDataContent();
                foreach (var prop in typeof(MDFClaimForm).GetProperties())
                {
                    var name = prop.Name;
                    if (prop.CanRead && prop.PropertyType == typeof(string) && !name.Contains("Error"))
                    {
                        var propValue = (string) prop.GetValue(claimForm, null);
                        if (propValue != null)
                        {
                            if (name.Equals("ActualClaimAmount"))
                            {
                                propValue = propValue.Replace(",", string.Empty).Replace("$", string.Empty);
                            }
                            if (name.Contains("Date"))
                            {
                                propValue = propValue + "T00:00:00Z";
                            }
                            formData.Add(new StringContent(propValue), name);
                        }
                    }
                }
                formData.Add(fileContent, "proofFile", file.FileName);
                var response = httpClient.PostAsync(sfApiRequest, formData).Result;
                if (!response.IsSuccessStatusCode)
                {
                    Console.Write(string.Format("There was a problem sending MDF Claim form to salesforce - {0} {1}", response.StatusCode, response.ReasonPhrase));
                    claimForm.SubmitError =
                        "There was a problem submitting your form. Please try again or contact your Carbonite representative.";
                    return false;
                }
                var result = JsonConvert.DeserializeObject<MDFResponse>(response.Content.ReadAsStringAsync().Result);
                if (!result.Success)
                {      
                    if (result.Errors.Contains("INVALID_EMAIL_ADDRESS"))
                    {
                        claimForm.SubmitError = "Please enter a valid email address.";
                    }
                    else if (result.Errors.Contains("currency"))
                    {
                        claimForm.SubmitError = "Currency fields accept only numeric or decimal values.";
                    }
                    else if (result.Errors.Contains("NUMBER_OUTSIDE_VALID_RANGE"))
                    {
                        claimForm.SubmitError = "Reimbursement Amount must be between $0 and $999,999,999.99.";
                    }
                    else
                    {
                        claimForm.SubmitError = result.Errors;
                    }
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.Write(string.Format("There was a problem sending MDF Claim form to salesforce - {0}", e));
                claimForm.SubmitError =
                    "There was a problem submitting your form. Please try again or contact your Carbonite representative.";
                return false;
            }
            return true;
        }
    }
}
