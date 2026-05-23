Feature: User Authentication
  As a user of the Finance Portfolio application
  I want to log in with my credentials
  So that I can access my private portfolio data

  Scenario: Successful login returns a JWT token
    Given a registered user with username "johndoe" and email "john@example.com"
    And the password "Password123!" is correct for that user
    When I log in with username "johndoe" and password "Password123!"
    Then a JWT token should be returned
    And the response username should be "johndoe"
    And the response email should be "john@example.com"
    And the response user id should not be empty

  Scenario: Login fails when the user does not exist
    Given no user exists with username "ghost"
    When I try to log in with username "ghost" and password "any"
    Then a not found error should be raised during login

  Scenario: Login fails when the password is incorrect
    Given a registered user with username "johndoe" and email "john@example.com"
    And the password "WrongPassword" is incorrect for that user
    When I try to log in with username "johndoe" and password "WrongPassword"
    Then a bad request error should be raised during login with message "aren't valid"

  Scenario: Generated JWT token contains the user id claim
    Given a registered user with username "johndoe" and email "john@example.com"
    And the password "Pass123!" is correct for that user
    When I log in with username "johndoe" and password "Pass123!"
    Then the JWT token should contain a "uid" claim with the user's id

  Scenario: Generated JWT token contains role claims
    Given a registered user with username "johndoe" and email "john@example.com"
    And the password "Pass123!" is correct for that user
    And the user has the role "Admin"
    When I log in with username "johndoe" and password "Pass123!"
    Then the JWT token should contain the role "Admin"
