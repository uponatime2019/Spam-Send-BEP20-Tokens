using System.Threading.Tasks;
using System;
using System.Threading;
using System.IO;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Util;
using System.Diagnostics;

namespace SpamSendToken
{
    public static class SendTokenHelper
    {
        static string PrivateKey = File.ReadAllText("private_key.txt");
        static string TargetWalletAddress = File.ReadAllText("target.txt");

        static string Erc20Abi = File.ReadAllText("erc20_abi.txt");
        const string CONTRACT_CAKE = "0x0e09fabb73bd3ade0a17ecc321fd13a19e81ce82";

        public static void SpamSendToken()
        {
            var account = new Account(PrivateKey);
            $"My address: {account.Address}".ConsoleWriteLine();
            $"Target wallet address: {TargetWalletAddress}".ConsoleWriteLine();

            RunAfter(1, async () =>
            {
                await Send();
            }, false, true);
        }

        static async Task Send()
        {
            try
            {
                var account = new Account(PrivateKey);
                var web3 = GetWeb3(account);

                var balance = await GetBalance(account.Address, CONTRACT_CAKE);
                if (balance > 0)
                {
                    $"Send {balance} CAKE".ConsoleWriteLine();
                    await SendToken(web3, balance, CONTRACT_CAKE, 18, TargetWalletAddress);
                }
                else
                {
                    $"No $CAKE".ConsoleWriteLine();
                }
            }
            catch (Exception ex)
            {
                ex.Message.ConsoleWriteLine();
            }
        }

        static async Task SendToken(Web3 web3, decimal amount, string tokenContract, int decimalsCount, string toAddress)
        {
            var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();

            var f = Fraction(amount);
            var transfer = new TransferFunction()
            {
                To = toAddress,
                TokenAmount = BigInteger.Pow(10, decimalsCount) * f.numerator / f.denominator,
                GasPrice = Web3.Convert.ToWei(3, UnitConversion.EthUnit.Gwei)
            };

            var transactionReceipt2 = await transferHandler.SendRequestAndWaitForReceiptAsync(tokenContract, transfer);
            var transactionHash = transactionReceipt2.TransactionHash;

            Debug.WriteLine(transactionHash);
        }


        static (BigInteger numerator, BigInteger denominator) Fraction(decimal d)
        {
            int[] bits = decimal.GetBits(d);
            BigInteger numerator = (1 - ((bits[3] >> 30) & 2)) *
                                   unchecked(((BigInteger)(uint)bits[2] << 64) |
                                             ((BigInteger)(uint)bits[1] << 32) |
                                              (BigInteger)(uint)bits[0]);
            BigInteger denominator = BigInteger.Pow(10, (bits[3] >> 16) & 0xff);
            return (numerator, denominator);
        }


        private static Web3 GetWeb3(Account acc = null)
        {
            if (acc != null)
            {
                return new Web3(acc, "https://bsc-dataseed.binance.org/");
            }
            else
            {
                return new Web3("https://bsc-dataseed.binance.org/");
            }
        }


        static async Task<decimal> GetBalance(string walletAddress, string contractAddress)
        {
            var web3 = GetWeb3();

            var balanceOfMessage = new BalanceOfFunction() { Owner = walletAddress };

            var decimals = await web3.Eth.GetContract(Erc20Abi, contractAddress).GetFunction("decimals").CallAsync<int>();

            //Creating a new query handler
            var queryHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();

            var balance = await queryHandler
                .QueryAsync<BigInteger>(contractAddress, balanceOfMessage)
                .ConfigureAwait(false);

            var balanceDecimal = (decimal)balance / (decimal)Math.Pow(10, decimals);
            return balanceDecimal;
        }

        #region Helpers
        static void ConsoleWriteLine(this string str)
        {
            Console.WriteLine(str);
        }

        static void RunAfter(double seconds, Action action, bool isOneTime = true, bool isRunImmediately = false, CancellationTokenSource cancelTokenSource = null)
        {
            int time = 0;
            try
            {
                time = Convert.ToInt32(seconds * 1000);
            }
            catch
            {
                time = int.MaxValue;
            }

            Timer timer = null;
            timer = new Timer((o) =>
            {
                try
                {
                    if (cancelTokenSource != null && cancelTokenSource.IsCancellationRequested)
                    {
                        timer.Dispose();
                        return;
                    }

                    if (isOneTime)
                    {
                        timer.Dispose();
                        //$"Timer stopped after {seconds}s".WriteToDebug();
                    }

                    action();
                }
                catch (Exception)
                {
                }
            }, null, isRunImmediately ? 0 : time, time);
        }
        #endregion
    }

    [Function("balanceOf", "uint256")]
    public class BalanceOfFunction : FunctionMessage
    {
        [Parameter("address", "_owner", 1)]
        public string Owner { get; set; }
    }

    [Function("transfer", "bool")]
    public class TransferFunction : FunctionMessage
    {
        [Parameter("address", "_to", 1)]
        public string To { get; set; }

        [Parameter("uint256", "_value", 2)]
        public BigInteger TokenAmount { get; set; }
    }

}
