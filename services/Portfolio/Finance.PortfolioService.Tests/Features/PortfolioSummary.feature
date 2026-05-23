Feature: Get Portfolio Summary
  As a portfolio owner
  I want to see a summary of all my holdings
  So that I can understand my total invested value, current value, and profit or loss

  Background:
    Given a user with id "11111111-1111-1111-1111-111111111111"

  Scenario: Portfolio is empty
    Given the user has no stocks in their portfolio
    When I request the portfolio summary
    Then the summary should show zero holdings
    And the total invested should be 0
    And the total PnL should be 0

  Scenario: Portfolio with a single stock at a gain
    Given the user owns stock "AAPL" in sector "Technology" with 1800 invested at a buying price of 180.00
    And the market data service returns a current price of 200.00 for "AAPL"
    When I request the portfolio summary
    Then the summary should contain 1 holding
    And the holding for "AAPL" should show a quantity of 10
    And the holding for "AAPL" should show a current value of 2000
    And the holding for "AAPL" should show a PnL of 200

  Scenario: Portfolio with multiple stocks calculates correct sector allocations
    Given the user owns stock "AAPL" in sector "Technology" with 3000 invested at a buying price of 150.00
    And the user owns stock "JPM" in sector "Finance" with 1000 invested at a buying price of 200.00
    And the market data service returns a current price of 150.00 for "AAPL"
    And the market data service returns a current price of 200.00 for "JPM"
    When I request the portfolio summary
    Then the sector allocation for "Technology" should be approximately 75 percent
    And the sector allocation for "Finance" should be approximately 25 percent

  Scenario: Market data is unavailable - fallback to stored price
    Given the user owns stock "NVDA" in sector "Technology" with 5000 invested at a buying price of 500.00
    And the stored price for "NVDA" is 500.00
    And the market data service is unavailable for "NVDA"
    When I request the portfolio summary
    Then the holding for "NVDA" should use the stored price of 500.00
