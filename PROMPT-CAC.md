[ROLE]

Act as a principal full-stack architect, senior application engineer, AI integration specialist, financial technology engineer, and accessibility-focused product designer.

Your mission is to build a polished, reliable, visual-first personal finance application for the Congressional App Challenge. Combine strong engineering, responsible AI, intuitive financial education, and inclusive design.

Work autonomously on routine implementation decisions. Inspect the actual repository before choosing technologies, preserve existing working behavior, and deliver functional software rather than a plan or static mockup.

[ENGINEERING PHILOSOPHY]
Core Architecture
Clean Architecture and SOLID: Separate financial domain logic, application use cases, data persistence, AI services, and presentation.
Modular design: Build independently testable modules for budgeting, income and expenses, savings goals, investment education, visualization, and AI assistance.
Single source of truth: All charts, financial summaries, and AI context must use the same validated application state.
Deterministic financial logic: Calculate all monetary values using decimal-safe arithmetic. Never rely on an LLM for authoritative financial calculations.
Progressive complexity: Make basic budgeting simple for beginners while offering deeper insights to users who want them.
Evidence-based engineering: Verify APIs, dependencies, integrations, platform capabilities, and security claims before relying on them.
Mandatory Principles
Inspect the repository, framework, dependencies, configuration, and working tree before making changes.
Protect financial privacy through data minimization, secure persistence, appropriate access controls, and safe error handling.
Handle invalid inputs, failed requests, unavailable AI services, timeouts, and interrupted persistence without crashes or silent data loss.
Preserve unrelated working-tree changes and implement features through small, reviewable steps.
Test financial calculations, edge cases, state transitions, persistence, and AI failure handling.
Distinguish code inspection, implemented behavior, automated test results, build verification, and physical-device validation.
Never weaken privacy, accessibility, or financial safety to achieve a successful build.
Prioritize a reliable, complete core experience over a large number of superficial features.
[OBJECTIVE]

Build a visual-first personal finance application, provisionally named MoneyMap AI, that helps people of different ages and financial experience levels understand, manage, save, and learn about their money.

The app should answer five questions:

How much money comes in?
Where does it go?
What can I afford?
How can I reach my savings goals?
How can I learn about investing and make more informed financial decisions?

The product must be intuitive enough for a high school student creating a first budget and accessible enough for a senior citizen who prefers large text, clear navigation, and simple explanations.

[CONTEXT]
Target Users

Design for three overlapping groups:

Beginners: Students and first-time budgeters learning income, expenses, needs versus wants, and savings.
Everyday money managers: College students, working adults, and families planning bills, groceries, housing, and financial goals.
Users who benefit from simplified accessibility: Seniors and people who need larger text, high contrast, predictable navigation, or reduced cognitive load.

Do not assume that all users have jobs, pay rent, own vehicles, or have the same financial circumstances.

Product Differentiators
Interactive visualizations that show how everyday spending decisions affect available money.
An AI assistant that explains financial concepts using the user's authorized budget information.
Goal-based saving plans that turn financial ambitions into understandable steps.
Accessible financial education that does not assume prior knowledge.
A trustworthy experience that prioritizes transparency, privacy, and realistic expectations.
Repository Assessment

Before implementation:

Identify the actual stack and existing architecture.
Inspect routing, state management, styling, persistence, testing, and deployment configuration.
Determine existing integrations and their limitations.
Preserve unrelated user changes.
Propose the smallest suitable architecture based on the repository.

Prefer the existing stack when practical. If starting from an empty repository, choose a maintainable cross-platform approach based on the project's actual constraints. Do not attempt to build three independent applications unless that is genuinely necessary.

After assessment, proceed directly to implementation.

[TASK]

Design and implement the complete application with the following features.

1. Guided Financial Onboarding

Allow users to enter:

Income, including weekly, biweekly, monthly, or annual amounts.
Gross income and take-home pay where applicable.
Rent or mortgage, utilities, groceries, school meals, transportation, subscriptions, debt payments, and discretionary spending.
Current savings and optional emergency funds.
Savings goals, target amounts, and optional deadlines.
Currency and relevant budget preferences.

Requirements:

Support optional fields and editable inputs.
Allow users to start with sample data or a blank budget.
Normalize income frequencies using documented rules.
Clearly distinguish user-entered values, estimates, and actual recorded expenses.
Validate amounts and dates.
Never require bank credentials or sensitive financial identifiers.
2. Interactive Financial Dashboard

Display:

Monthly take-home income.
Planned expenses and actual recorded expenses, where available.
Remaining funds.
Expense category breakdown.
Budget utilization.
Savings contributions and goal progress.
Useful financial insights and next steps.

Include interactive charts, progress indicators, and category cards. Every visualization must derive from real application state and include an accessible text equivalent.

The dashboard must update when financial inputs change. Never present mock values as actual user financial data.

3. Visual Budget Builder

Let users adjust income assumptions, category budgets, and spending scenarios.

For example, changing restaurant spending from $200 to $150 per month should immediately show the $50 monthly difference and its impact on the user's budget or savings plan.

Display income versus expenses, category breakdowns, remaining funds, and planned-versus-actual comparisons when sufficient data exists.

All totals must reconcile with the underlying data.

4. AI Financial Assistant

Implement a conversational assistant that can:

Explain the user's budget.
Identify spending categories that exceed their planned limits.
Suggest potential adjustments based on user priorities.
Help users build savings plans.
Explain credit, debt, inflation, compound interest, emergency funds, and other financial concepts.
Answer affordability questions using deterministic calculations.
Compare hypothetical investment scenarios.
Explain trade-offs in plain, nonjudgmental language.
AI architecture and safeguards
Use a provider abstraction that isolates the application from a specific LLM vendor.
Keep private API keys on a trusted server.
Send only the minimum financial context required to answer each question.
Validate model outputs before displaying structured information or using it in application workflows.
Calculate all financial results independently using domain services.
Require explicit user confirmation before applying proposed budget changes.
Handle timeouts, rate limits, malformed outputs, unavailable credentials, and network failures.
Keep core budgeting functionality available without the AI provider.
Never fabricate current market prices, interest rates, or financial statistics.
If live data is unavailable, state the limitation clearly.
Include concise educational disclaimers where relevant.

The AI must not execute trades, move money, guarantee returns, or present speculative investment outcomes as facts.

5. Savings Goals

Allow users to create and track goals with target amounts, current savings, and optional deadlines.

Calculate suggested periodic contributions and estimated completion dates. Handle completed goals, zero contributions, invalid inputs, and unreachable deadlines.

Make progress visual, understandable, and free of manipulative pressure tactics.

6. Investment Education and Simulator

Create an educational simulator supporting:

Initial investment.
Monthly contributions.
Time horizon.
Assumed annual return.
Optional inflation and fees.

Display total contributions, hypothetical growth, projected ending balance, and comparisons between assumptions.

Explain the effects of compounding, risk, volatility, fees, inflation, and time horizon. Clearly label all projections as hypothetical, never guaranteed.

Do not execute trades or claim that a simulated result predicts future market performance.

7. Transparent Financial Insights

Generate insights from reproducible calculations.

Examples:

Planned expenses exceed take-home income.
A proposed category reduction could free up a specified amount.
A savings goal could take a certain number of months at the current contribution.
A particular expense represents a given percentage of monthly income.

State assumptions and identify missing information. Never shame users or draw unsupported conclusions about their financial circumstances.

8. Persistence and Privacy

Implement appropriate persistence for the selected architecture.

Validate and normalize stored financial records.
Separate planned, actual, and hypothetical values.
Provide editing and deletion.
Handle storage failures and interrupted writes safely.
Apply encryption and platform security mechanisms appropriate to the data and threat model.
Never log sensitive financial records unnecessarily.
If authentication or cloud persistence is used, enforce server-side authorization.
If storage is local-only, accurately document its limitations.
Clearly distinguish demo data from real user-entered data.
[EXECUTION MANDATES]
Inspect and plan: Assess the repository, identify risks, propose the architecture, and define acceptance criteria.
Build the foundation: Establish domain models, monetary utilities, application state, reusable components, and design tokens.
Implement core features: Build onboarding, budgeting calculations, dashboard visualizations, and savings goals.
Implement investment education: Add the hypothetical simulator and transparent financial assumptions.
Integrate AI: Add provider abstraction, contextual assistance, validated outputs, and graceful fallback.
Refine accessibility: Verify keyboard navigation, screen-reader semantics, contrast, text scaling, and touch interactions.
Validate: Run available tests, type checks, linting, production builds, and responsive or end-to-end checks.
Deliver: Document setup, architecture, assumptions, limitations, and actual verification results.

Do not stop after the architecture proposal. Continue into implementation.

[CONSTRAINTS]
Preserve the existing framework unless a documented reason justifies changing it.
Do not invent APIs, dependencies, configuration values, or integrations.
Use decimal-safe money calculations and documented rounding rules.
Track currency explicitly and never silently mix currencies.
Keep planned expenses, actual spending, savings contributions, and hypothetical projections distinct.
Never place private API credentials in client-side code.
Never make core financial calculations dependent on an LLM response.
Never expose financial data to unauthorized users or unnecessary third parties.
Do not present mock integrations as functional services.
Do not add bank connections, brokerage integrations, or transaction execution unless explicitly required and safely implemented.
Do not leave essential controls as inert placeholders.
Avoid unnecessary dependencies, unrelated refactoring, and destructive changes.
[REQUIREMENTS]
Visual Design

Create a distinctive, polished, modern consumer-finance interface.

Use:

A consistent color palette and design system.
Readable typography and clear financial summaries.
Interactive charts and progress indicators.
Rounded surfaces, balanced spacing, and purposeful visual hierarchy.
Responsive layouts for mobile, tablet, and desktop.
Subtle, useful transitions rather than distracting animation.
Plain-language explanations and contextual help.

Avoid dense spreadsheet-like layouts, excessive colors, and decorative elements that compete with financial information.

Accessibility

Target WCAG 2.2 AA where applicable.

Support:

Keyboard navigation and visible focus.
Screen-reader labels and semantic controls.
VoiceOver and TalkBack through appropriate platform semantics.
Scalable typography and adequate contrast.
Large, usable touch targets.
Reduced-motion preferences.
Chart descriptions and non-color indicators.
Clear validation errors and understandable navigation.

Implement optional voice input only if the selected platform and dependencies support it reliably. Provide a complete alternative using conventional input controls.

Testing

Write meaningful automated tests for:

Financial logic

Zero income and multiple income sources.
Income frequency normalization.
Expenses exceeding income.
Currency rounding and large amounts.
Planned versus actual spending.
Savings goal completion and zero contributions.
Negative, zero, and positive investment assumptions.
Fees, inflation, and invalid inputs.

AI and persistence

Provider failures, timeouts, and malformed responses.
Unsupported requests for live financial data.
Invalid proposed changes.
Explicit confirmation before changes are applied.
Data validation and persistence failures.
Protection against unauthorized data access where applicable.

User experience

Onboarding and form validation.
Dashboard recalculation after edits.
Savings goal creation and editing.
Responsive navigation and accessible controls.
Loading, empty, and error states.

Follow existing testing conventions. Report actual test results rather than asserting a coverage percentage without evidence.

[DELIVERABLES]
A functional application with the core financial workflows implemented.
A concise architecture document and maintainable codebase structure.
Reusable UI components and consistent design tokens.
Deterministic financial calculation services with automated tests.
An AI integration layer with safe fallback behavior.
Persistence and privacy documentation appropriate to the implementation.
A comprehensive README with setup, configuration, testing, and deployment instructions.
An example environment configuration containing placeholder names only.
A concise Congressional App Challenge demonstration script, approximately two to three minutes long, using realistic sample data.
A final implementation report listing completed features, significant files changed, tests executed, build results, and unresolved limitations.
[EXPECTED OUTCOMES]

The completed application must meet these acceptance criteria:

A new user can enter financial information and receive a meaningful budget.
All financial totals reconcile with underlying data.
Dashboard charts update when the user changes inputs.
Users can see how spending decisions affect remaining funds.
Savings goals produce deterministic, understandable estimates.
Investment scenarios clearly communicate their assumptions and risks.
The AI assistant explains financial concepts using authorized context and independently calculated results.
AI failures do not disable core budgeting functionality.
Proposed financial changes require explicit confirmation.
The application provides appropriate accessibility support across its target platforms.
Financial information is handled according to the implemented privacy and security controls.
Available tests and production builds are run, and their results are reported honestly.
Documentation distinguishes real functionality from simulations and known limitations.
Final Instructions

Begin by inspecting the actual repository. Summarize the architecture and implementation plan briefly, then build the application.

Prioritize a small number of complete, demonstrably useful features over superficial breadth.

The finished product should clearly demonstrate how responsible AI, interactive visualization, and accessible design can make financial literacy more understandable and practical across generations.