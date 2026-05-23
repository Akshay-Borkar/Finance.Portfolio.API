Feature: Stock Price Updated Consumer - Alert Triggering
  As the alert system
  When a stock price update message is received
  I want to evaluate all active alerts for that ticker
  So that users are notified when their target prices are crossed

  Scenario: Triggers an Above alert when price meets or exceeds target
    Given an active "Above" alert for ticker "AAPL" with target price 180
    When a price update message arrives for "AAPL" with price 185
    Then the alert should be marked as triggered
    And an AlertTriggered event should be published with direction "above"
    And the published event should carry the current price 185

  Scenario: Triggers a Below alert when price meets or falls below target
    Given an active "Below" alert for ticker "TSLA" with target price 200
    When a price update message arrives for "TSLA" with price 190
    Then the alert should be marked as triggered
    And an AlertTriggered event should be published with direction "below"

  Scenario: Does not trigger when price has not yet met an Above target
    Given an active "Above" alert for ticker "NVDA" with target price 600
    When a price update message arrives for "NVDA" with price 580
    Then the alert should not be triggered
    And no AlertTriggered event should be published

  Scenario: Does not trigger when price has not yet met a Below target
    Given an active "Below" alert for ticker "META" with target price 300
    When a price update message arrives for "META" with price 310
    Then the alert should not be triggered
    And no AlertTriggered event should be published

  Scenario: Triggers exactly at the boundary price for an Above alert
    Given an active "Above" alert for ticker "AMZN" with target price 200
    When a price update message arrives for "AMZN" with price 200
    Then the alert should be marked as triggered

  Scenario: No active alerts for the ticker - nothing happens
    Given there are no active alerts for ticker "GOOG"
    When a price update message arrives for "GOOG" with price 150
    Then no AlertTriggered event should be published

  Scenario: Multiple alerts for the same ticker are all evaluated
    Given an active "Above" alert for ticker "AAPL" with target price 180
    And an active "Above" alert for ticker "AAPL" with target price 175
    When a price update message arrives for "AAPL" with price 190
    Then 2 alerts should be triggered
    And 2 AlertTriggered events should be published
