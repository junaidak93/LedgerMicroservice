# Assignment: The Ledger Microservice (AI-Native)

## Objective
Build a functional .NET microservice to manage a ledger of user transactions. We are looking to evaluate how you leverage modern AI tools to architect, implement, and iterate on a production-grade service.

## The Workflow Requirement
Please use Cursor (or a similar AI-powered IDE like GitHub Copilot Workspace) to complete this task.

- Structured Prompts: We encourage using a "Closed-Loop" workflow (e.g., using /plan to define architecture and /execute for implementation).
- Transparency: Include your .cursorrules file or a PROMPTS.md log detailing the key commands and architectural decisions you made with the AI.
- Commit History: Provide a GitHub repository link. Do not squash your commits; we want to see the iterative progression and how the project evolved.

## Technical Requirements
- Framework: .NET 8 / ASP.NET Core.
- Database: PostgreSQL using Entity Framework Core.
- Context: Keeper is a leading Earned Wage Access (EWA) platform. Design this microservice to track money moving in and out of user accounts with the precision and reliability required for a financial technology platform.


## Functional Requirements
- Transaction Management: Create a RESTful API to Create, Read, Update, and Delete transactions. Each record must include:
  - UserId
  - Amount
  - Type (Incoming/Outgoing)
  - Fee
- Statistics API: Provide an endpoint to retrieve:
  - Global Stats: Total volume processed and total fees collected across all users.
  - User Stats: Current balance and transaction count for a specific UserId.
  - The "Silent" Task: Design this as if it were a production service. We have intentionally left out specific implementation details regarding validation, data integrity, and error handling to see how you approach these as a Senior Engineer.

## The Live Technical Review
Following your submission, we will hold a live session where you will share your Cursor screen. We will work together to implement 2-3 real-world pivots (e.g., refactoring logic, handling concurrency, or adding complex business rules) to see how you and your AI agent handle evolving requirements.
