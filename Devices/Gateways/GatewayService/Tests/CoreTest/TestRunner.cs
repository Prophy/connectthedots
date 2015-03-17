﻿//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

namespace Microsoft.ConnectTheDots.Test
{
    using System;
    using System.Configuration;
    using Newtonsoft.Json;
    using Microsoft.ConnectTheDots.Common;
    using Microsoft.ConnectTheDots.Gateway;

    //--//

    public class TestRunner
    {
        static void Main( string[] args )
        {
            // we do not need a tunable logger, but this is a nice way to test it...
            TunableLogger logger = TunableLogger.FromLogger(
                    SafeLogger.FromLogger( TestLogger.Instance )
                    );

            TunableLogger.LoggingLevel loggingLevel = TunableLogger.LevelFromString( ConfigurationManager.AppSettings.Get( "LoggingLevel" ) );

            logger.Level = ( loggingLevel != TunableLogger.LoggingLevel.Undefined ) ? loggingLevel : TunableLogger.LoggingLevel.Errors;


            /////////////////////////////////////////////////////////////////////////////////////////////
            // Test core service 
            //
            CoreTest t2 = new CoreTest( logger );
            t2.Run( );
            Console.WriteLine( String.Format( "Core Test completed" ) );

            //////////////////////////////////////////////////////////////////////////////////////////////
            // Test Web service and core service 
            //
            SensorDataContract sensorData = RandomSensorDataGenerator.Generate( );
            string serializedData = JsonConvert.SerializeObject( sensorData );

            WebServiceTest t1 = new WebServiceTest( "http://localhost:8000/GatewayService/API/Enqueue?jsonData=" + serializedData, logger );
            t1.Run( );
            Console.WriteLine( String.Format( "WebService Test completed, {0} messages sent", t1.TotalMessagesSent ) );

            /////////////////////////////////////////////////////////////////////////////////////////////
            // Test Socket
            //
            SocketTest t3 = new SocketTest( logger );
            t3.Run( );
            Console.WriteLine( String.Format( "Socket Test completed" ) );

            // wait for logging tasks to complete
            Console.WriteLine( "Press enter to exit" );
            Console.ReadLine( );
        }
    }
}
