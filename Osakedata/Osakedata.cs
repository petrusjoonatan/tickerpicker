using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Osakedata;

/// <summary>
/// This program provides stock buying recommendations based on technical indicators that meet
/// specific criteria for favorable buying opportunities. The stock price data is
/// retrieved from the AlphaVantage API, and the analysis is conducted within the code.
/// The analysis is based on the following technical indicators: RSI, EMA, SMA and VOLUME
/// Program returns stocks that fulfill all these criteria.
/// </summary>
/// @author Petrus Genas
/// @version 0.00
public static class Program
{
    /// <summary>
    /// Main triggers the process of fetching stock data and making buy recommendations.
    /// </summary>
    private static void Main()
    {
        string apiKey = "";
        string recommendations = GivePurchaseRecommendationFulfilled(GetTickers(), apiKey);
        Console.WriteLine(recommendations);
    }
    
    /// <summary>
    /// Returns all stock tickers as an array to be checked for criteria in the analysis.
    /// </summary>
    /// <returns>An array of stock ticker symbols.</returns>
    public static string[] GetTickers()
    {
        string[] tickers = {"TSLA"};
        return tickers;
    }
    
    /// <summary>
    /// Analyzes the given stock tickers and provides a purchase recommendation for those that meet all criteria.
    /// </summary>
    /// <param name="tickers">Array of stock ticker symbols to analyze.</param>
    /// <param name="apiKey">API key for accessing Alphavantage data.</param>
    /// <returns>A recommendation string if any stock meets all criteria.</returns>
    public static string GivePurchaseRecommendationFulfilled(string[] tickers, string apiKey)
    {
        for (int i = 0; i < tickers.Length; i++)
        {   
            double stockEma = GetMovingAverage("ema", $"{tickers[i]}", "daily", "50", apiKey);
            double stockSma = GetMovingAverage("sma", $"{tickers[i]}", "daily", "200", apiKey);
            
            if (CheckAllCriteria(tickers[i], apiKey, stockEma, stockSma))
            {
                string buyToday = $"Osta {tickers[i]} tänään!";
                return buyToday;
            }
        }

        return "Istu käsien päällä, tänään ei mitään ostettavaa!";
    }

    /// <summary>
    /// Checks if the stock meets all defined criteria
    /// </summary>
    /// <param name="ticker">Stock ticker, e.g., "TSLA".</param>
    /// <param name="apiKey">API key for accessing AlphaVantage data.</param>
    /// <param name="ema">The Exponential Moving Average value</param>
    /// <param name="sma">The Simple Moving Average value</param>
    /// <returns><c>true</c> if the stock fulfills all criteria; otherwise, <c>false</c>.</returns>
    public static bool CheckAllCriteria(string ticker, string apiKey, double ema, double sma)
    {
        if ((CheckRelativeStrengthIndex(ticker, apiKey)) && CompareMovingAverages(ema, sma) && CompareVolume(ticker, apiKey))
            return true;
        else
        {
            return false;
        }
    }
    
    /// <summary>
    /// Checks if today's trading volume is greater than the 10-day average volume for a specified stock.
    /// </summary>
    /// <param name="symbol">The stock ticker symbol, e.g., "TSLA".</param>
    /// <param name="apiKey">API key for accessing AlphaVantage data.</param>
    /// <returns><c>true</c> if today's volume is greater than the 10-day average; otherwise, <c>false</c>.</returns>
    public static bool CompareVolume(string symbol, string apiKey)
    {
        string queryUrl =
            $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={apiKey}";

        Uri queryUri = new Uri(queryUrl);

        double[] volumes = new double[10];

        using (WebClient client = new WebClient())
        {
            string pricedata = client.DownloadString(queryUri);

            string keyWord = "\"5. volume\": \"";

            int startIndex = pricedata.IndexOf("\"5. volume\": \"", StringComparison.Ordinal) + keyWord.Length;

            int endIndex = pricedata.IndexOf('\"', startIndex);

            for (int i = 0; i < volumes.Length; i++)
            {
                string volumeInString = pricedata.Substring(startIndex, endIndex - startIndex);
                double volume = CleanAndConvertToDouble(volumeInString);
                volumes[i] = volume;
                startIndex = pricedata.IndexOf("\"5. volume\": \"", endIndex + 1, StringComparison.Ordinal) + keyWord.Length;
                endIndex = pricedata.IndexOf('\"', startIndex);
            }
        }
        
        double tenDayAverageVolume = volumes.Take(10).Average();

        if (volumes[0] > tenDayAverageVolume)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    /// <summary>
    /// Retrieves the moving average (either SMA or EMA) for the specified stock symbol, interval, and time period.
    /// </summary>
    /// <param name="function">The type of moving average to retrieve, e.g., "sma" or "ema".</param>
    /// <param name="symbol">The stock ticker symbol, e.g., "TSLA".</param>
    /// <param name="interval">The interval for the moving average, e.g., "daily" or "5min".</param>
    /// <param name="timeperiod">The time period over which the average is calculated, e.g., "50" or "200".</param>
    /// <param name="apiKey">API key for accessing AlphaVantage data.</param>
    /// <returns>The requested moving average as a double.</returns>
    private static double GetMovingAverage(string function, string symbol, string interval, string timeperiod, string apiKey)
    {
        string queryUrl =
            $"https://www.alphavantage.co/query?function={function}&symbol={symbol}&interval={interval}&time_period={timeperiod}&series_type=open&apikey={apiKey}";

        Uri queryUri = new Uri(queryUrl);

        using WebClient client = new WebClient();
        {
            string movingAverage = client.DownloadString(queryUri);
            
            string smaKey = "\"SMA\": \"";
            string emaKey = "\"EMA\": \"";
            
            int startIndex = movingAverage.IndexOf(smaKey, StringComparison.Ordinal);

            if (startIndex == -1)
            {
                startIndex = movingAverage.IndexOf(emaKey, StringComparison.Ordinal);
                startIndex += emaKey.Length;
            }
            else
            {
                startIndex += smaKey.Length;
            }
            
            int endIndex = movingAverage.IndexOf("\"", startIndex, StringComparison.Ordinal);
            
            string movingAverageString = movingAverage.Substring(startIndex, endIndex - startIndex);
            double movingAverageDouble = CleanAndConvertToDouble(movingAverageString);
            return movingAverageDouble;
        }
    }

    /// <summary>
    /// Retrieves the Relative Strength Index (RSI) for the specified stock ticker and returns its value.
    /// </summary>
    /// <param name="symbol">The stock ticker symbol, e.g., "TSLA".</param>
    /// <param name="apiKey">API key for accessing AlphaVantage data.</param>
    /// <returns>The RSI value as a <c>double</c>; returns <c>double.MaxValue</c> if retrieval fails.</returns>
    private static double GetRelativeStrengthIndex(string symbol, string apiKey)
    {
        string queryUrl =
            $"https://www.alphavantage.co/query?function=RSI&symbol={symbol}&interval=daily&time_period=14&series_type=close&apikey={apiKey}";

        Uri queryUri = new Uri(queryUrl);

        using (WebClient client = new WebClient())
        {
            string relativeStrengthIndex = client.DownloadString(queryUri);
            
            string rsiKey = "\"RSI\": \"";
            
            int startIndex = relativeStrengthIndex.IndexOf(rsiKey, StringComparison.Ordinal);

            if (startIndex == -1) return Double.MaxValue;
            startIndex += rsiKey.Length;

            int endIndex = relativeStrengthIndex.IndexOf("\"", startIndex, StringComparison.Ordinal);
            
            if (endIndex == -1) return Double.MaxValue;
            
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