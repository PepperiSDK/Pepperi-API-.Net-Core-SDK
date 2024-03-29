﻿using Pepperi.SDK.Helpers;
using Pepperi.SDK.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Pepperi.SDK.Model.Fixed;
using Pepperi.SDK.Model;
using System.IO;
using System.IO.Compression;
using Pepperi.SDK.Contracts;
using System.Threading;
using System.Reflection;

namespace Pepperi.SDK.Endpoints.Base
{
    /// <summary>
    /// Crud API 
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TModelKey"></typeparam>
    public class BaseEndpoint<TModel, TModelKey>
        where TModel : class
    {
        #region properties

        private string ApiBaseUri { get; set; }
        private IAuthentication Authentication { get; set; }
        private ILogger Logger { get; set; }
        private string ResourceName { get; set; }
        private bool IsInternalAPI { get; set; } = false;

        #endregion

        #region constructor

        protected BaseEndpoint(string ApiBaseUri, IAuthentication Authentication, ILogger Logger, string ResourceName, bool IsInternalAPI = false)
        {
            this.ApiBaseUri = ApiBaseUri;
            this.Authentication = Authentication;
            this.Logger = Logger;
            this.ResourceName = ResourceName;
            this.IsInternalAPI = IsInternalAPI;
        }

        #endregion

        #region Public methods


        /// <summary>
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="include_nested">indicates whether references (1:many) data should be upserted. reference (1:1) fields are upserted.</param>
        /// <returns></returns>
        /// <remarks>
        /// Post and Get use the same model
        /// Operation may do insert or Partial update depanding on the value of ExternalID \ InternalID
        /// Insert
        ///     if the External\InternalID are not given in the request
        ///     The model properties with null value are not serialzied by APIClient. Server populates them with default value.
        ///Partial update
        ///     If the External\InternalID is given in the request
        ///     The model properties with null value are not serialzied by APIClient. server updates the values of the properties which are not null.
        ///Regarding Reference:
        ///     used in 1:1 relations when both entities may exist indepandently.
        ///     To attach relation call upsert (setting the x.Reference.Data.ExternalID or x.Reference.Data.InternalID)
        ///     To detach relation call upsert (setting the x.Rerefere.Data to null)
        ///     To read the reference call find with full_mode = true
        ///Regarding References:
        ///     used in 1:many relations when child entities may exist without parent entity.
        ///     To set relations call upsert (simply deletes all exsiting relations and set the relations we send on upsert)
        ///         (setting the x.References.Data.Add ( { InternalID=  .... ExternalID = .... })
        ///     To delete all the relations call upset *setting the x.Reference.Data to empty collection.
        ///     To read the references call find with includ_nested =  true
        ///     
        ///     used in 1:many relations when the child entity can not exist without the parent entity  
        ///             (Contact can not exist without the account)
        ///             in that case, as you create the child entity (eg, Contact) you use its reference Property  (ExternalID or InternalID) to associte the child with existing parent.   [Do not use the ParentExternalId property of the child, since it is used obly in Bulk).
        /// 
        /// </remarks>
        public TModel Upsert(TModel Model, bool? include_nested = null)
        {
            string RequestUri = ResourceName;

            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            if (include_nested.HasValue) { dicQueryStringParameters.Add("include_nested", include_nested.Value.ToString()); }

            string postBody = PepperiJsonSerializer.Serialize(Model);                                       //null values are not serialized
            string contentType = "application/json";
            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.PostStringContent(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    postBody,
                    contentType,
                    accept
                    );

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            TModel result = PepperiJsonSerializer.DeserializeOne<TModel>(PepperiHttpClientResponse.Body);
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="where"></param>
        /// <param name="order_by"></param>
        /// <param name="page"></param>
        /// <param name="page_size"></param>
        /// <param name="include_nested"></param>
        /// <param name="include_nested">populate the References propeties of the result(1:many)</param>
        /// <param name="full_mode">populate the Reference propeties of the result (1:1)</param>
        /// <param name="fields"></param>
        /// <returns>set of objects</returns>
        public IEnumerable<TModel> Find(string where = null, string order_by = null, int? page = null, int? page_size = null, bool? include_nested = null, bool? full_mode = null, bool? include_deleted = null, string fields = null, bool? is_distinct = null)
        {
            string RequestUri = ResourceName;

            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            if (where != null) { dicQueryStringParameters.Add("where", where); }
            if (order_by != null) { dicQueryStringParameters.Add("order_by", order_by); }
            if (page.HasValue) { dicQueryStringParameters.Add("page", page.Value.ToString()); }
            if (page_size.HasValue) { dicQueryStringParameters.Add("page_size", page_size.Value.ToString()); }
            if (include_nested.HasValue) { dicQueryStringParameters.Add("include_nested", include_nested.Value.ToString()); }
            if (full_mode.HasValue) { dicQueryStringParameters.Add("full_mode", full_mode.Value.ToString()); }
            if (include_deleted.HasValue) { dicQueryStringParameters.Add("include_deleted", include_deleted.Value.ToString()); }
            if (fields != null) { dicQueryStringParameters.Add("fields", fields); }
            if (is_distinct != null) { dicQueryStringParameters.Add("is_distinct", fields); }

            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.Get(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    accept
                    );

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            IEnumerable<TModel> result = PepperiJsonSerializer.DeserializeCollection<TModel>(PepperiHttpClientResponse.Body);
            return result;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fields">comma seperated list of the fields to include</param
        /// <param name="include_nested">populate the References propeties of the result</param>
        /// <param name="full_mode">populate the Reference propeties of the result</param>
        /// <returns>the object</returns>
        public TModel FindByID(TModelKey id, string fields = null, bool? include_nested = null, bool? full_mode = null)
        {
            string RequestUri = ResourceName + "//" + HttpUtility.UrlEncode(id.ToString());
            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            if (fields != null) { dicQueryStringParameters.Add("fields", fields); }
            if (include_nested.HasValue) { dicQueryStringParameters.Add("include_nested", include_nested.Value.ToString()); }
            if (full_mode.HasValue) { dicQueryStringParameters.Add("full_mode", full_mode.Value.ToString()); }
            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.Get(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    accept);

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            TModel result = PepperiJsonSerializer.DeserializeOne<TModel>(PepperiHttpClientResponse.Body);   //Api returns single object
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="externalId"></param>
        /// <param name="fields">comma seperated list of the fields to include</param
        /// <param name="include_nested">populate the References propeties of the result</param>
        /// <param name="full_mode">populate the Reference propeties of the result</param>
        /// <returns>the object</returns>
        public TModel FindByExternalID(string externalId, string fields = null, bool? include_nested = null, bool? full_mode = null)//not relevant for: inventory and user defined tables
        {
            string RequestUri = ResourceName + "//externalid//" + HttpUtility.UrlEncode(externalId);
            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();

            if (fields != null) { dicQueryStringParameters.Add("fields", fields); }
            if (include_nested.HasValue) { dicQueryStringParameters.Add("include_nested", include_nested.Value.ToString()); }
            if (full_mode.HasValue) { dicQueryStringParameters.Add("full_mode", full_mode.Value.ToString()); }
            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.Get(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    accept);

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            TModel result = PepperiJsonSerializer.DeserializeOne<TModel>(PepperiHttpClientResponse.Body);   //Api returns single object
            return result;
        }


        /// <summary>
        /// </summary>
        /// <param name="UUID"></param>
        /// <param name="fields">comma seperated list of the fields to include</param>
        /// <param name="include_nested">populate the References propeties of the result</param>
        /// <param name="full_mode">populate the Reference propeties of the result</param>
        /// <returns>the object</returns>
        public TModel FindByUUID(string UUID, string fields = null, bool? include_nested = null, bool? full_mode = null)//not relevant for: inventory and user defined tables
        {
            string RequestUri = ResourceName + "//UUID//" + HttpUtility.UrlEncode(UUID);
            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            if (fields != null) { dicQueryStringParameters.Add("fields", fields); }
            if (include_nested.HasValue) { dicQueryStringParameters.Add("include_nested", include_nested.Value.ToString()); }
            if (full_mode.HasValue) { dicQueryStringParameters.Add("full_mode", full_mode.Value.ToString()); }
            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.Get(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    accept);

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            TModel result = PepperiJsonSerializer.DeserializeOne<TModel>(PepperiHttpClientResponse.Body);   //Api returns single object
            return result;
        }




        /// <summary>
        /// </summary>
        /// <param name="UUID"></param>
        /// <param name="include_nested"></param>
        /// <param name="full_mode"></param>
        /// <returns>
        /// json returned by Pepperi API
        /// </returns>
        /// <remarks>
        /// 1. The method is usefull if you want to get data as json (eg, to get TSAs that are not defined in TModel)
        /// </remarks>
        public string FindJson_ByUUID(string UUID, bool? include_nested = null, bool? full_mode = null)
        {
            string RequestUri = ResourceName + "//UUID//" + HttpUtility.UrlEncode(UUID);
            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            if (include_nested.HasValue) { dicQueryStringParameters.Add("include_nested", include_nested.Value.ToString()); }
            if (full_mode.HasValue) { dicQueryStringParameters.Add("full_mode", full_mode.Value.ToString()); }
            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.Get(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    accept);

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            //TModel result = PepperiJsonSerializer.DeserializeOne<TModel>(PepperiHttpClientResponse.Body);   //Api returns single object
            return PepperiHttpClientResponse.Body;
        }




        public bool DeleteByID(TModelKey id)
        {
            string RequestUri = ResourceName + "//" + HttpUtility.UrlEncode(id.ToString());
            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();

            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.Delete(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    accept);

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            bool result = PepperiJsonSerializer.DeserializeOne<bool>(PepperiHttpClientResponse.Body);   //Api returns bool right now. true-when resource is found and deleted. Otherwise, false.

            return result;
        }


        public bool DeleteByExternalID(string externalId)
        {
            string RequestUri = ResourceName + "//externalid//" + HttpUtility.UrlEncode(externalId);
            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();

            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.Delete(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    accept);

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            bool result = PepperiJsonSerializer.DeserializeOne<bool>(PepperiHttpClientResponse.Body);   //Api returns bool right now. true-when resource is found and deleted. Otherwise, false.

            return result;
        }





        /// <summary>
        /// Group And Aggregate function
        /// </summary>
        /// <param name="select">comma seperated list of aggregate functions. eg: "max(UnitPrice),min(UnitPrice)".   Supported functions: min,max,av,count.</param>
        /// <param name="group_by">Optional. comma seperated list of fields to group_by</param>
        /// <param name="where">Optioal</param>
        /// <returns>
        /// Array with a dictionary per group. Each dicionary holds the selected values for that group, eg: max_UnitPrice.
        /// </returns>
        /// <example>
        /// if you want to  group   transaction_lines by Transaction.InternalID 
        ///                 and     get for each group:                         max(UnitPrice),min(UnitPrice),avg(UnitPrice),count(UnitPrice)
        ///                 
        /// Then, call this method with:                  
        ///             group_by    ="Transaction.InternalID"
        ///             select      ="max(UnitPrice) as xxx,min(UnitPrice),avg(UnitPrice),count(UnitPrice)"
        ///             where       ="Transaction.InternalID>2066140676"
        ///             
        /// The result:
        ///                         [   {"Transaction.InternalID": 65064336,"xxx": 23.0,"min_UnitPrice": 19.0,"avg_UnitPrice": 21.0,"count_UnitPrice": 2.0},
        ///		                        {"Transaction.InternalID": 65064316,"xxx": 23.0,"min_UnitPrice": 23.0,"avg_UnitPrice": 23.0,"count_UnitPrice": 1.0},
        ///		                    ]        
        ///
        /// Note:       This method will send http request:     Get         https://.../V1.0//totals/transaction_lines?select=max(UnitPrice) as xxx,min(UnitPrice),avg(UnitPrice),count(UnitPrice)&group_by=Transaction.InternalID&where=Transaction.InternalID>2066140676 
        /// </example>
        public Dictionary<string, object>[] GetTotals(string select, string group_by = null, string where = null)
        {

            string RequestUri = "totals/" + ResourceName;

            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            if (select != null) { dicQueryStringParameters.Add("select", select); }
            if (group_by != null) { dicQueryStringParameters.Add("group_by", group_by); }
            if (where != null) { dicQueryStringParameters.Add("where", where); }

            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.Get(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    accept);

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            Dictionary<string, object>[] result = PepperiJsonSerializer.DeserializeOne<Dictionary<string, object>[]>(PepperiHttpClientResponse.Body);   //Api returns array of dictionary
            return result;

        }


        public long GetCount(string where = null, bool? include_deleted = null)
        {
            #region read first Page

            string RequestUri = ResourceName;

            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            if (where != null) { dicQueryStringParameters.Add("where", where); }
            if (include_deleted.HasValue) { dicQueryStringParameters.Add("include_deleted", include_deleted.Value.ToString()); }
            dicQueryStringParameters.Add("include_count", "true");

            dicQueryStringParameters.Add("page", "1");
            dicQueryStringParameters.Add("page_size", "1");

            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.Get(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    accept
                    );

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            #endregion

            #region Parse result (value of X-Pepperi-Total-Pages header)

            string headerKey = "X-Pepperi-Total-Pages";
            bool header_exists = PepperiHttpClientResponse.Headers.ContainsKey(headerKey);
            if (header_exists == false)
            {
                throw new PepperiException("Failed retrieving Total pages from response.");
            }

            IEnumerable<string> headerValue = PepperiHttpClientResponse.Headers[headerKey];
            if (headerValue == null || headerValue.Count() != 1)
            {
                throw new PepperiException("Failed retrieving Total pages from response.");
            }

            string resultAsString = headerValue.First();
            long result = 0;
            bool parsedSucessfully = long.TryParse(resultAsString, out result);


            if (!parsedSucessfully)
            {
                throw new PepperiException("Failed retrieving Total pages from response.");
            }

            #endregion

            return result;
        }




        #region Async Read


        /// <summary>
        /// Provides Find functionality async (without paging)
        /// </summary>
        /// <param name="where"></param>
        /// <param name="order_by"></param>
        /// <param name="include_deleted"></param>
        /// <param name="fields"></param>
        /// <param name="is_distinct"></param>
        /// <returns>resonse with JobID to send to GetExportJobInfo</returns>
        public ExportAsyncResponse ExportAsync(string where = null, string order_by = null, bool? include_deleted = null, string fields = null, bool? is_distinct = null)
        {
            var ExportAsyncRequest = new ExportAsyncRequest();
            ExportAsyncRequest.where = where;
            ExportAsyncRequest.order_by = order_by;
            ExportAsyncRequest.include_deleted = include_deleted;
            ExportAsyncRequest.fields = fields;
            ExportAsyncRequest.is_distinct = is_distinct;

            string RequestUri = string.Format("export/{0}", ResourceName);

            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();

            string postBody = PepperiJsonSerializer.Serialize(ExportAsyncRequest);  //null values are not serialzied.
            string contentType = "application/json";
            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.PostStringContent(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    postBody,
                    contentType,
                    accept
                    );

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            ExportAsyncResponse result = PepperiJsonSerializer.DeserializeOne<ExportAsyncResponse>(PepperiHttpClientResponse.Body);
            return result;
        }


        /// <summary>
        /// </summary>
        /// <param name="JobID">JobID returned by ExportAsync</param>
        /// <returns>Status</returns>
        public GetExportJobInfoResponse GetExportJobInfo(string JobID)
        {
            string RequestUri = string.Format("export/jobinfo/{0}", HttpUtility.UrlEncode(JobID));

            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.Get(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    accept);

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            GetExportJobInfoResponse result = PepperiJsonSerializer.DeserializeOne<GetExportJobInfoResponse>(PepperiHttpClientResponse.Body);   //Api returns single object
            return result;

        }


        /// <summary>
        /// Pools the status of the job until completion or timeout
        /// </summary>
        /// <param name="JobID"></param>
        /// <param name="poolingInternvalInMs"></param>
        /// <param name="numberOfPoolingAttempts"></param>
        /// <returns>the JobInfoResponse or throws timeout exception</returns>
        public GetExportJobInfoResponse WaitForExportJobToComplete(string JobID, int poolingInternvalInMs = 1000, int numberOfPoolingAttempts = 60 * 5)
        {
            bool ExportAsyncCompleted = false;
            int getJobInfoAttempts = 0;
            bool poolingTimeout = false;

            GetExportJobInfoResponse GetExportJobInfoResponse = null;

            while (ExportAsyncCompleted == false && poolingTimeout == false)
            {
                if (getJobInfoAttempts > 0)
                {
                    Thread.Sleep(poolingInternvalInMs);
                }

                GetExportJobInfoResponse = GetExportJobInfo(JobID);

                ExportAsyncCompleted = GetExportJobInfoResponse.StatusCode == 1 || GetExportJobInfoResponse.StatusCode == 3;  //   Succeeded Failed          
                getJobInfoAttempts++;

                poolingTimeout = (getJobInfoAttempts == numberOfPoolingAttempts);
            }

            if (poolingTimeout == true)
            {
                throw new PepperiException(string.Format("WaitForExportJobToComplete timed out. poolingInternvalInMs={0}|numberOfPoolingAttempts={1} ms", poolingInternvalInMs, numberOfPoolingAttempts));
            }

            //exportAsyncCompleted
            return GetExportJobInfoResponse;
        }


        #endregion



        #region Async Upsert


        /// <summary>
        /// Upsert of collection As json or csv zip
        /// </summary>
        /// <param name="data"></param>
        /// <param name="OverrideMethod"></param>
        /// <param name="BulkUploadMethod"></param>
        /// <param name="fieldsToUpload"></param>
        /// <param name="FilePathToStoreZipFile">Optional. We can store the generated zip file for debugging purpose.</param>
        /// <param name="SaveZipFileInLocalDirectory"></param>
        /// <param name="SubTypeID">The Sub type id. You can get it from the metadata.</param>
        /// <returns></returns>
        public BulkUploadResponse BulkUpload(IEnumerable<TModel> data, eOverwriteMethod OverrideMethod, eBulkUploadMethod BulkUploadMethod, IEnumerable<string> fieldsToUpload, bool SaveZipFileInLocalDirectory = false, string SubTypeID = "")
        {
            //validate input
            if (fieldsToUpload == null || fieldsToUpload.Count() == 0)
            {
                throw new PepperiException("No header fields  are specified.");
            }


            BulkUploadResponse BulkUploadResponse = null;

            switch (BulkUploadMethod)
            {
                case eBulkUploadMethod.Json:
                    {
                        BulkUploadResponse = BulkUpload_OfJson(data, OverrideMethod, fieldsToUpload);
                        break;
                    }
                case eBulkUploadMethod.Zip:
                    {
                        string FilePathToStoreZipFile = null;
                        if (SaveZipFileInLocalDirectory)
                        {
                            string AssemblyLocation = Assembly.GetExecutingAssembly().Location;
                            string AssemblyPath = Path.GetDirectoryName(AssemblyLocation);
                            string zipDirectory = AssemblyPath;
                            string zipFileName = "BulkUpload_" + this.ResourceName + ".zip";
                            FilePathToStoreZipFile = Path.Combine(zipDirectory, zipFileName);
                        }

                        BulkUploadResponse = BulkUploadOfZip(data, OverrideMethod, fieldsToUpload, FilePathToStoreZipFile, SubTypeID);
                        break;
                    }
                default:
                    {
                        throw new PepperiException("Invalid argument: the upload method is not supported.");
                    }
            }

            return BulkUploadResponse;
        }

        public BulkUploadResponse BulkUpload(string csvFilePath, eOverwriteMethod OverwriteMethod, Encoding fileEncoding, string SubTypeID = "", string FilePathToStoreZipFile = null)
        {
            byte[] fileAsBinary = File.ReadAllBytes(csvFilePath);
            return BulkUpload(fileAsBinary, OverwriteMethod, fileEncoding, SubTypeID, FilePathToStoreZipFile);
        }

        public BulkUploadResponse BulkUpload(byte[] fileAsBinary, eOverwriteMethod OverwriteMethod, Encoding fileEncoding, string SubTypeID = "", string FilePathToStoreZipFile = null)
        {
            bool isToAddBOM = true;
            // UTF8 byte order mark is: 0xEF,0xBB,0xBF
            if (fileAsBinary[0] == 0xEF && fileAsBinary[1] == 0xBB && fileAsBinary[2] == 0xBF)
            {
                isToAddBOM = false;
            }
            byte[] fileAsUtf8 = Encoding.Convert(fileEncoding, Encoding.UTF8, fileAsBinary);
            string fileAsUtf8String = System.Text.Encoding.UTF8.GetString(fileAsUtf8);
            byte[] fileAsZipInUTF8 = PepperiFlatSerializer.UTF8StringToZip(fileAsUtf8String, FilePathToStoreZipFile, isToAddBOM);

            BulkUploadResponse result = BulkUploadOfZip(fileAsZipInUTF8, OverwriteMethod, SubTypeID);
            return result;
        }

        public GetBulkJobInfoResponse GetBulkJobInfo(string JobID)
        {
            string RequestUri = string.Format("bulk/jobinfo/{0}", HttpUtility.UrlEncode(JobID));
            if (IsInternalAPI)
                RequestUri = string.Format("AsyncAPI/jobinfo/{0}", HttpUtility.UrlEncode(JobID));
            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.Get(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    accept);

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            GetBulkJobInfoResponse result = PepperiJsonSerializer.DeserializeOne<GetBulkJobInfoResponse>(PepperiHttpClientResponse.Body);   //Api returns single object
            return result;

        }


        /// <summary>
        /// Pools the status of the job until completion or timeout
        /// </summary>
        /// <param name="JobID"></param>
        /// <param name="poolingInternvalInMs"></param>
        /// <param name="numberOfPoolingAttempts"></param>
        /// <returns>the JobInfoResponse  or throws timeout exception</returns>
        public GetBulkJobInfoResponse WaitForBulkJobToComplete(string JobID, int poolingInternvalInMs = 1000, int numberOfPoolingAttempts = 60 * 5)
        {
            bool bulkUpsertCompleted = false;
            int getJobInfoAttempts = 0;
            bool poolingTimeout = false;

            GetBulkJobInfoResponse GetBulkJobInfoResponse = null;

            while (bulkUpsertCompleted == false && poolingTimeout == false)
            {
                if (getJobInfoAttempts > 0)
                {
                    Thread.Sleep(poolingInternvalInMs);
                }

                GetBulkJobInfoResponse = GetBulkJobInfo(JobID);

                bulkUpsertCompleted = GetBulkJobInfoResponse.Status != "Not Started" && GetBulkJobInfoResponse.Status != "In Progress";

                getJobInfoAttempts++;

                poolingTimeout = (getJobInfoAttempts == numberOfPoolingAttempts);
            }


            if (poolingTimeout == true)
            {
                throw new PepperiException("Bulk Upload did not complete within " + poolingInternvalInMs * numberOfPoolingAttempts + " ms");
            }

            //bulkUpsertCompleted
            return GetBulkJobInfoResponse;
        }

        #endregion


        #endregion

        #region TODEL: using Pepperi metadata endpoint instead meta_data (in MedaData_BaseEndpoint)

        public IEnumerable<FieldMetadata> GetFieldsMetaData()
        {
            string RequestUri = string.Format(@"metadata/{0}", this.ResourceName);
            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.Get(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    accept);

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            IEnumerable<FieldMetadata> result = PepperiJsonSerializer.DeserializeCollection<FieldMetadata>(PepperiHttpClientResponse.Body);
            result = result.OrderBy(o => o.Name);
            return result;
        }

        /// <summary></summary>
        /// <returns>the sub types of this resource</returns>
        /// <remarks>
        /// 1. some resources (eg, transaction and activity) are "abstract types".
        ///    the concrete types "derive" from them:               eg, sales transaction, invoice  derive from transaction     (with header and lines) 
        ///                                                         eg, visit                       derive from activity        (with header)
        ///    the concrete class cusom fields are modeled as TSA filed
        /// 2. All the types are returned by metadata endpoint 
        /// 3. Activities, Transactions, transaction_lines are "abstract" type. Acconut is concrete typr that may be derived by SubType.
        ///     The concrete type is identified by ActivityTypeID
        ///     For Bulk or CSV upload, the ActivityTypeID is sent on the url   
        ///     For single Upsert,      the ActivityTypeID is set on the object
        ///     The values of the ActivityTypeID are taken from the SubTypeMetadata action
        /// </remarks>
        public IEnumerable<TypeMetadata> GetSubTypesMetadata()
        {
            string RequestUri = string.Format("metadata");
            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.Get(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    accept);

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            IEnumerable<TypeMetadata> result = PepperiJsonSerializer.DeserializeCollection<TypeMetadata>(PepperiHttpClientResponse.Body);
            result = result.Where(subTypeMetadata =>
                                    subTypeMetadata.Type == ResourceName &&
                                    subTypeMetadata.SubTypeID != null &&
                                    subTypeMetadata.SubTypeID.Length > 0
                            ).
                            ToList();

            return result;
        }

        public string CreateUserDefinedField(UserDefinedField UserDefinedField, string SubTypeID = null)
        {

            string RequestUri =
               (SubTypeID == null || SubTypeID.Length == 0) ?
                   string.Format(@"metadata/{0}", this.ResourceName) :
                   string.Format(@"metadata/{0}/{1}", ResourceName, SubTypeID);

            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            string postBody = PepperiJsonSerializer.Serialize(UserDefinedField);                                       //null values are not serialized
            string contentType = "application/json";
            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.PostStringContent(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    postBody,
                    contentType,
                    accept
                    );

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            string result = PepperiJsonSerializer.DeserializeOne<string>(PepperiHttpClientResponse.Body);
            return result;
        }



        #endregion

        #region Private methods

        private BulkUploadResponse BulkUpload_OfJson(IEnumerable<TModel> data, eOverwriteMethod OverwriteMethod, IEnumerable<string> fieldsToUpload)
        {
            FlatModel FlatModel = PepperiFlatSerializer.MapDataToFlatModel(data, fieldsToUpload, "''");

            string RequestUri = string.Format("bulk/{0}/json", ResourceName);
            if (IsInternalAPI)
                RequestUri = string.Format("AsyncAPI/{0}/json", ResourceName);
            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            dicQueryStringParameters.Add("overwrite", OverwriteMethod.ToString());

            string postBody = PepperiJsonSerializer.Serialize(FlatModel);                         //null values are not serialzied.
            string contentType = "application/json";
            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.PostStringContent(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    postBody,
                    contentType,
                    accept
                    );

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            BulkUploadResponse result = PepperiJsonSerializer.DeserializeOne<BulkUploadResponse>(PepperiHttpClientResponse.Body);
            return result;
        }

        private BulkUploadResponse BulkUploadOfZip(IEnumerable<TModel> data, eOverwriteMethod OverwriteMethod, IEnumerable<string> fieldsToUpload, string FilePathToStoreZipFile, string SubTypeID = null)
        {
            FlatModel FlatModel = PepperiFlatSerializer.MapDataToFlatModel(data, fieldsToUpload, "''");

            string CsvFileAsString = PepperiFlatSerializer.FlatModelToCsv(FlatModel);

            byte[] CsvFileAsZipInUTF8 = PepperiFlatSerializer.UTF8StringToZip(CsvFileAsString, FilePathToStoreZipFile);

            BulkUploadResponse result = BulkUploadOfZip(CsvFileAsZipInUTF8, OverwriteMethod, SubTypeID);
            return result;
        }

        /// <summary>
        /// reusable method
        /// </summary>
        /// <param name="fileAsZipInUTF8"></param>
        /// <param name="OverwriteMethod"></param>
        /// <param name="SubTypeID">
        ///     usually empty value. 
        ///     we use the parameter for sub types 
        ///         eg, sales order that derive from transaction
        ///         eg, invoice     that derive from transaction
        ///         eg, visit       that derive from activity 
        ///     in that case we take the SubTypeID from the metadata endpoint (see GetSubTypesMetadata method)
        ///     The custom fields are TSA fields
        ///     </param>
        /// <returns></returns>
        /// <remarks>
        /// 1. the post body is in UTF8
        /// </remarks>
        private BulkUploadResponse BulkUploadOfZip(byte[] fileAsZipInUTF8, eOverwriteMethod OverwriteMethod, string SubTypeID = null)
        {
            string bulkPrefix = "bulk";
            if (IsInternalAPI)
                bulkPrefix = "AsyncAPI";
            string RequestUri =
                (SubTypeID == null || SubTypeID.Length == 0) ?
                    string.Format(bulkPrefix + "/{0}/csv_zip", ResourceName) :
                    string.Format(bulkPrefix + "/{0}/{1}/csv_zip", ResourceName, SubTypeID);                                //eg, for transaction or activity

            Dictionary<string, string> dicQueryStringParameters = new Dictionary<string, string>();
            dicQueryStringParameters.Add("overwrite", OverwriteMethod.ToString());


            byte[] postBody = fileAsZipInUTF8;
            string contentType = "application/zip";
            string accept = "application/json";

            PepperiHttpClient PepperiHttpClient = new PepperiHttpClient(this.Authentication, this.Logger);
            PepperiHttpClientResponse PepperiHttpClientResponse = PepperiHttpClient.PostByteArraycontent(
                    ApiBaseUri,
                    RequestUri,
                    dicQueryStringParameters,
                    postBody,
                    contentType,
                    accept
                    );

            PepperiHttpClient.HandleError(PepperiHttpClientResponse);

            BulkUploadResponse result = PepperiJsonSerializer.DeserializeOne<BulkUploadResponse>(PepperiHttpClientResponse.Body);
            return result;
        }


        #endregion
    }
}


