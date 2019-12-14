//-----------------------------------------------------------------------
// <copyright file="CosmosError.cs" company="Weather Balloon">
//     Copyright (c) Duncan Dickinson. BSD 2-Clause License.
// </copyright>
//-----------------------------------------------------------------------

namespace WeatherBalloon.ObservationLoader.Cosmos
{
    using System.Collections.Generic;
    using System.Linq;
    using MongoDB.Driver;

    /// <summary>
    /// Basic Cosmos Error object.
    /// </summary>
    public class CosmosError
    {
        /// <summary>The error codes that indicate a retry is possible.</summary>
        public static readonly HashSet<int> RetriableErrorCodes = new HashSet<int> { 16500 };

        /// <summary>Error index.</summary>
        public int Index { get; set; }

        /// <summary>The error code.</summary>
        public int ErrorCode { get; set; } = 0;

        /// <summary>The milliseconds allowed before the next try.</summary>
        public int RetryAfterMs { get; set; }

        /// <summary>Error details.</summary>
        public string Details { get; set; } = string.Empty;

        /// <summary>True if the operation can be retried, false otherwise.</summary>
        public bool IsRetriable
        {
            get
            {
                return RetriableErrorCodes.Contains(ErrorCode);
            }
        }

        /// <summary>Parses an exception stack.</summary>
        /// <param name="exceptionObject">The exception object.</param>
        /// <returns>A list of <see cref="CosmosError"/>s.</returns>
        public static IList<CosmosError> ParseBulkWriteException(MongoBulkWriteException exceptionObject)
        {
            List<CosmosError> result = new List<CosmosError>();
            foreach (var we in exceptionObject.WriteErrors)
            {
                result.Add(ParseBulkWriteError(we));
            }

            return result;
        }

        /// <summary>Handles the parsing of a BulkWriteError.</summary>
        /// <param name="writeError">The error object.</param>
        /// <returns>An instance of <see cref="CosmosError"/>.</returns>
        public static CosmosError ParseBulkWriteError(BulkWriteError writeError)
        {
            var result = ParseCosmosErrorMessage(writeError.Message);
            result.Index = writeError.Index;
            return result;
        }

        /// <summary>Parses an error message into a <see cref="CosmosError"/>.</summary>
        /// <param name="message">The error message.</param>
        /// <returns>An instance of <see cref="CosmosError"/>.</returns>
        public static CosmosError ParseCosmosErrorMessage(string message)
        {
            var result = new CosmosError();
            foreach (var el in message.Split(','))
            {
                var attr = el.Trim().Split('=');

                if (attr.Count() != 2)
                {
                    continue;
                }

                if (attr[0] == "Error")
                {
                    result.ErrorCode = int.Parse(attr[1]);
                }

                if (attr[0] == "RetryAfterMs")
                {
                    result.RetryAfterMs = int.Parse(attr[1]);
                }
            }

            return result;
        }
    }
}
