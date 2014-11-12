﻿//******************************************************************************************************
//  VerboseLevelExtensions.cs - Gbtc
//
//  Copyright © 2014, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  11/12/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSF.Diagnostics
{
    /// <summary>
    /// Extension methods for the <see cref="VerboseLevel"/>.
    /// </summary>
    public static class VerboseLevelExtensions
    {
        /// <summary>
        /// Gets verbose levels that will not show debug flags.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static VerboseLevel NonDebug(this VerboseLevel value)
        {
            return VerboseLevel.All ^ VerboseLevel.DebugLow ^ VerboseLevel.DebugNormal ^ VerboseLevel.DebugHigh;
        }

    }
}
