Feature: Get User Alerts
  As a registered user
  I want to retrieve my price alerts with pagination
  So that I can review and manage them

  Background:
    Given an alert user with id "33333333-3333-3333-3333-333333333333"

  Scenario: Retrieve first page of alerts
    Given the user has 2 alerts in the repository
    When I request alerts page 1 with page size 10
    Then the result should contain 2 alerts
    And the total count should be 2

  Scenario: Returns empty list when user has no alerts
    Given the user has 0 alerts in the repository
    When I request alerts page 1 with page size 10
    Then the result should contain 0 alerts
    And the total count should be 0

  Scenario: Pagination parameters are forwarded to the repository
    Given the user has 0 alerts in the repository
    When I request alerts page 3 with page size 5
    Then the repository should be queried with page 3 and page size 5

  Scenario: Alert DTO fields are mapped correctly
    Given the user has an alert for "NVDA" with condition "Above" target 500 that is triggered
    When I request alerts page 1 with page size 10
    Then the first alert in the result should have ticker "NVDA"
    And the first alert condition should be "Above"
    And the first alert target price should be 500
    And the first alert should be marked as triggered
