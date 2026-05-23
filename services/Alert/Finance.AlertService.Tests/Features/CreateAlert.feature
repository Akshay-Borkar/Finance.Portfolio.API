Feature: Create Stock Price Alert
  As a registered user
  I want to set a price alert on a stock
  So that I am notified when the price crosses my target

  Background:
    Given an alert user with id "33333333-3333-3333-3333-333333333333"
    And the user's email is "investor@example.com"

  Scenario: Successfully create an Above alert
    When I create an alert for ticker "AAPL" with condition "Above" and target price 200
    Then an alert should be saved with ticker "AAPL"
    And the alert condition should be "Above"
    And the alert target price should be 200
    And the alert should not be triggered
    And the returned alert id should not be empty

  Scenario: Successfully create a Below alert
    When I create an alert for ticker "TSLA" with condition "Below" and target price 150
    Then an alert should be saved with ticker "TSLA"
    And the alert condition should be "Below"

  Scenario: Alert ticker is always stored in uppercase
    When I create an alert for ticker "msft" with condition "Above" and target price 400
    Then an alert should be saved with ticker "MSFT"

  Scenario: Invalid condition string causes an error
    When I try to create an alert for ticker "GOOG" with condition "Sideways" and target price 100
    Then an argument error should be raised

  Scenario Outline: Validation rejects invalid alert data
    When I try to submit an alert with ticker "<ticker>" condition "<condition>" target price <price> and email "<email>"
    Then the alert submission should be rejected
    And the validation error should mention "<field>"

    Examples:
      | ticker        | condition | price | email             | field     |
      |               | Above     | 100   | user@test.com     | Ticker    |
      | AAPL          |           | 100   | user@test.com     | Condition |
      | AAPL          | sideways  | 100   | user@test.com     | Condition |
      | AAPL          | Above     | 0     | user@test.com     | TargetPrice |
      | AAPL          | Above     | -10   | user@test.com     | TargetPrice |
      | AAPL          | Above     | 100   | not-an-email      | UserEmail |
      | AAPL          | Above     | 100   |                   | UserEmail |
      | TOOLONGTICKER | Above     | 100   | user@test.com     | Ticker    |
