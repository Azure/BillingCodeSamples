﻿//----------------------------------------------------------------------------------------------
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//----------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using ISVDemoUsage.Models;

namespace ISVDemoUsage
{
    public class DataAccess : DbContext
    {
        public DataAccess() : base("DataAccess") { }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<PerUserTokenCache> PerUserTokenCacheList { get; set; }
    }
//    public class DataAccessInitializer : System.Data.Entity.DropCreateDatabaseIfModelChanges<DataAccess>
    public class DataAccessInitializer : System.Data.Entity.CreateDatabaseIfNotExists<DataAccess>
    {

    }
}