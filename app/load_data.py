#!/usr/bin/env python3

from binance.client import Client


client = Client()

def get_btc_price():
    btc_price = client.get_symbol_ticker(symbol="BTCUSDT")
    return float(btc_price["price"])

if __name__ == "__main__":
    price = get_btc_price()
    print(f"Current BTCUSDT {price}")