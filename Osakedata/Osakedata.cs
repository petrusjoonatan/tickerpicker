using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Osakedata;

/// <summary>
/// A program that selects stocks or indices based on their technical indicators
/// which meet the criteria for a good buying opportunity. Price data is fetched from Alphavantage API,
/// and analysis is performed in the code. The program returns a list of stocks that fulfill all pre-defined criteria 
/// for a favorable buying opportunity. Following indicators are used for the analysis: RSI, EMA50/SMA200 Golden Cross
/// and volume action.
/// </summary>
/// @author Petrus Genas
/// @version 0.00
public static class Program
{
    /// <summary>
    /// Main function retrieves stock price and technical indicator data through methods and analyzes them.
    /// </summary>
    private static void Main()
    {
        
        string apiKey = "XIYAPYGZC2E7VFJU";
        
        string recommendations = GivePurchaseRecommendationFulfilled(GetTickers(), apiKey);
        Console.WriteLine(recommendations);
        
    }
    
    public static string[] GetTickers()
    {
        string[] tickers = {"BABA", "MSFT", "NVDA"};
        return tickers;
    }

    public static string GivePurchaseRecommendationFulfilled(string[] tickers, string apiKey)
    {
        for (int i = 0; i < tickers.Length; i++)
        {   
            double stockEma = GetMovingAverage("ema", $"{tickers[i]}", "daily", "50", apiKey);
            double stockSma = GetMovingAverage("sma", $"{tickers[i]}", "daily", "50", apiKey);
            
            if (CheckAllCriteria(tickers[i], apiKey, stockEma, stockSma))
            {
                string buyToday = $"Osta {tickers[i]} tänään!";
                return buyToday;
            }
        }

        return null;
    }

    public static bool CheckAllCriteria(string ticker, string apiKey, double ema, double sma)
    {
        if ((CheckRelativeStrengthIndex(ticker, apiKey)) && CompareMovingAverages(ema, sma))
            return true;
        else
        {
            return false;
        }
    }
    

    /// <summary>
    /// Retrieves the latest price and volume data for the specified stock ticker.
    /// </summary>
    /// <param name="symbol">Stock ticker, e.g., "TSLA".</param>
    /// <param name="apiKey">API key for accessing Alphavantage data.</param>
    /// <returns>Returns price and volume data for the stock.</returns>
    private static string GetPriceAndVolume(string symbol, string apiKey)
    {
        string queryUrl =
            $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={apiKey}";

        Uri queryUri = new Uri(queryUrl);

        using (WebClient client = new WebClient())
        {
            // Fetch price data
            string pricedata = client.DownloadString(queryUri);

            // Split data into an array based on newline characters
            string[] lines = pricedata.Split('\n');
            
            // Extract relevant rows containing the latest trading day's price and volume
            string data = string.Join("\n", lines.Skip(10).Take(5));

            // Return the selected rows
            return data;
        }
    }

    /// <summary>
    /// Retrieves the moving average for the specified stock ticker, type, interval, and time period.
    /// </summary>
    /// <param name="function">Type of moving average (SMA or EMA).</param>
    /// <param name="symbol">Stock ticker, e.g., "TSLA".</param>
    /// <param name="interval">Interval, e.g., "daily" or "5min".</param>
    /// <param name="timeperiod">Time period, e.g., 200 or 50.</param>
    /// <param name="apiKey">API key for accessing Alphavantage data.</param>
    /// <returns>Returns the moving average value (SMA or EMA).</returns>
    private static double GetMovingAverage(string function, string symbol, string interval, string timeperiod, string apiKey)
    {
        string queryUrl =
            $"https://www.alphavantage.co/query?function={function}&symbol={symbol}&interval={interval}&time_period={timeperiod}&series_type=open&apikey={apiKey}";

        Uri queryUri = new Uri(queryUrl);

        using WebClient client = new WebClient();
        {
            // Fetch moving average data
            string movingAverage = client.DownloadString(queryUri);

            // Define the keys for SMA and EMA in the data.
            string smaKey = "\"SMA\": \"";
            string emaKey = "\"EMA\": \"";

            // Check whether it's SMA or EMA
            int startIndex = movingAverage.IndexOf(smaKey);

            if (startIndex == -1)
            {
                startIndex = movingAverage.IndexOf(emaKey);
                if (startIndex == -1)
                {
                    return Double.MaxValue;
                }
                // If EMA is found, proceed with that
                startIndex += emaKey.Length;
            }
            else
            {
                // If SMA is found, proceed with that
                startIndex += smaKey.Length;
            }

            // Find the closing quote for the SMA or EMA value
            int endIndex = movingAverage.IndexOf("\"", startIndex); 
            if (endIndex == -1) return double.MaxValue;

            // Return the SMA or EMA value
            string movingAverageString = movingAverage.Substring(startIndex, endIndex - startIndex);
            double movingAverageDouble = CleanAndConvertToDouble(movingAverageString);
            return movingAverageDouble;
        }
    }

    /// <summary>
    /// Retrieves the Relative Strength Index (RSI) for the specified stock ticker.
    /// </summary>
    /// <param name="symbol">Stock ticker, e.g., "TSLA".</param>
    /// <param name="apiKey">API key for accessing Alphavantage data.</param>
    /// <returns>Returns the RSI value.</returns>
    private static double GetRelativeStrengthIndex(string symbol, string apiKey)
    {
        string queryUrl =
            $"https://www.alphavantage.co/query?function=RSI&symbol={symbol}&interval=daily&time_period=14&series_type=close&apikey={apiKey}";

        Uri queryUri = new Uri(queryUrl);

        using (WebClient client = new WebClient())
        {
            // Fetch RSI data
            string relativeStrengthIndex = client.DownloadString(queryUri);

            // Store the key where the RSI value is found
            string rsiKey = "\"RSI\": \"";
            int startIndex = relativeStrengthIndex.IndexOf(rsiKey);

            if (startIndex == -1) return Double.MaxValue;
            startIndex += rsiKey.Length;

            int endIndex = relativeStrengthIndex.IndexOf("\"", startIndex);
            if (endIndex == -1) return Double.MaxValue;

            // Return the RSI value
            string rsiString = relativeStrengthIndex.Substring(startIndex, endIndex - startIndex);
            double rsiDouble = CleanAndConvertToDouble(rsiString);
            
            return rsiDouble;
        }
    }
    
    /// <summary>
    /// Cleans the given string by removing all characters except numbers and the decimal point,
    /// replaces the decimal point with a comma, and converts the result to a double value.
    /// </summary>
    /// <param name="value">A string that contains a number</param>
    /// <returns>A cleaned string converted to a double value.</returns>
    private static double CleanAndConvertToDouble(string value)
    {
        string cleanedValue = Regex.Replace(value.Trim(), "[^\\d.]", "").Replace('.', ',');
        return double.Parse(cleanedValue);
    }

    /// <summary>
    /// Compares two moving averages: the Exponential Moving Average (EMA50) and the Simple Moving Average (SMA200).
    /// </summary>
    /// <param name="ema">The Exponential Moving Average value</param>
    /// <param name="sma">The Simple Moving Average value</param>
    /// <returns>
    /// <c>true</c> if the Exponential Moving Average is greater than or equal to the Simple Moving Average; 
    /// <c>false</c> if the EMA is less than the SMA.
    /// </returns>
    private static bool CompareMovingAverages(double ema, double sma)
    {
        
        if (ema >= sma)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    /// <summary>
    /// Determines whether the Relative Strength Index (RSI) for a given stock symbol is below or equal to 42
    /// </summary>
    /// <param name="symbol">Stock ticker, e.g., "TSLA".</param>
    /// <param name="apiKey">API key for accessing Alphavantage data.</param>
    /// <returns>
    /// <c>true</c> if the Relative Strength Index (RSI) is less than or equal to 42; 
    /// <c>false</c> if the RSI is greater than 42.
    /// </returns>
    private static bool CheckRelativeStrengthIndex(string symbol, string apiKey)
    {
        double relativeStrengthIndex = GetRelativeStrengthIndex(symbol, apiKey);

        if (relativeStrengthIndex <= 42.0)
        {
            return true;
        }
        else
        {
            return false;
        }
        
    }


}
