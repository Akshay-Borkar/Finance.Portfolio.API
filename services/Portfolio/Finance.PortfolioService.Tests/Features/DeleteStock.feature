Feature: Delete Stock from Portfolio
  As a portfolio owner
  I want to delete a stock from my portfolio
  So that stocks I no longer hold are removed along with their investment history

  Background:
    Given a user with id "11111111-1111-1111-1111-111111111111"

  Scenario: Successfully delete a stock with existing investments
    Given a stock with id "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" and ticker "AAPL" owned by the current user
    And the stock has 2 existing investments
    When I delete the stock
    Then all 2 investments should be deleted
    And the stock itself should be deleted
    And a StockRemoved event should be published for ticker "AAPL"

  Scenario: Successfully delete a stock with no investments
    Given a stock with id "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb" and ticker "META" owned by the current user
    And the stock has 0 existing investments
    When I delete the stock
    Then no investments should be deleted
    And the stock itself should be deleted
    And a StockRemoved event should be published for ticker "META"

  Scenario: Cannot delete a stock that does not exist
    Given no stock exists with id "cccccccc-cccc-cccc-cccc-cccccccccccc"
    When I try to delete that stock
    Then a not found error should be raised

  Scenario: Cannot delete a stock owned by another user
    Given a stock with id "dddddddd-dddd-dddd-dddd-dddddddddddd" and ticker "GOOG" owned by a different user
    When I try to delete that stock
    Then a bad request error should be raised with message "permission"
