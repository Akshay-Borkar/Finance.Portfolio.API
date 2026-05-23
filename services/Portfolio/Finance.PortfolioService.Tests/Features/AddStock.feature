Feature: Add Stock to Portfolio
  As a portfolio owner
  I want to add a stock to my portfolio
  So that I can track its performance over time

  Background:
    Given a user with id "11111111-1111-1111-1111-111111111111"
    And a sector with id "22222222-2222-2222-2222-222222222222"

  Scenario: Successfully add a stock when market data is available
    Given the market data service returns a price of 185.50 for ticker "AAPL"
    When I add a stock with ticker "aapl" and name "Apple Inc." to the portfolio
    Then a new stock should be created with ticker "AAPL"
    And the stock should have a current price of 185.50
    And a StockAdded event should be published for ticker "AAPL"
    And the returned stock id should not be empty

  Scenario: Add a stock when market data service is unavailable
    Given the market data service is unavailable for ticker "TSLA"
    When I add a stock with ticker "tsla" and name "Tesla Inc." to the portfolio
    Then a new stock should be created with ticker "TSLA"
    And the stock should have a current price of 0
    And a StockAdded event should still be published for ticker "TSLA"

  Scenario: Ticker is always stored in uppercase
    Given the market data service returns a price of 400.00 for ticker "MSFT"
    When I add a stock with ticker "msft" and name "Microsoft" to the portfolio
    Then a new stock should be created with ticker "MSFT"

  Scenario: Stock is associated with the correct user and sector
    Given the market data service returns a price of 150.00 for ticker "GOOG"
    When I add a stock with ticker "GOOG" and name "Alphabet" to the portfolio
    Then the stock should belong to user "11111111-1111-1111-1111-111111111111"
    And the stock should belong to sector "22222222-2222-2222-2222-222222222222"
