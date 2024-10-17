# Distributed Task Scheduler with Kafka and SQL Server

This project is a distributed task scheduling system designed for handling tasks across multiple worker instances with flexible scheduling and partitioning via Kafka. It supports task categorization, dynamic scaling, and recurrence management (including Cron expressions) for executing tasks across different types of infrastructure, such as Kubernetes and Elasticsearch.

## Features
- **Task Scheduling**: Schedule tasks with flexible intervals (seconds to daily) or using Cron expressions.
- **Kafka Integration**: Tasks are dispatched via Kafka, with message partitioning based on task types for efficient load balancing.
- **Task Categories and Types**: Clear separation between task categories (e.g., Kubernetes, Elasticsearch) and subtypes (e.g., HealthCheck, NodeCheck).
- **Distributed Processing**: Workers grouped by task category consume tasks from Kafka, ensuring only one worker in a group processes a task at a time.
- **Scalable Architecture**: Kafka partitions allow for horizontal scaling of worker instances, distributing task load efficiently.
- **Task Tracking**: Status of each task (e.g., Pending, Processing, Completed) is tracked, along with details on which worker processed the task.

## Requirements
- **.NET Core**
- **Kafka** (for task distribution and partitioning)
- **SQL Server** (for storing task schedules, types, and processing status)

## Solution Components

- **FlexTaskScheduler.Models**: Contains the data models used across the solution, including `ScheduledTask`, `TaskExecutionHistory`, `TaskType`, and `TaskCategory`.

- **FlexTaskScheduler.ScheduleProcessor**: Responsible for producing tasks to Kafka topics based on the schedule. It includes:
  - `TaskProducer`: Publishes scheduled tasks to Kafka.
  - `Worker`: Background service that processes and schedules tasks.

- **KubernetesTaskConsumer**: Consumes messages from Kafka topics and processes them based on the task type. It includes:
  - `KafkaConsumerService`: Listens to Kafka topics and routes messages to appropriate processors.
  - Topic-specific processors for handling Kubernetes-related tasks.


## Setup
### Prerequisites
- Kafka Cluster: Install Kafka or use a managed Kafka service.
- SQL Server: You can use a locally installed or cloud-hosted SQL Server.
- .NET Core SDK: Ensure you have the .NET Core SDK installed for building and running the project.

### Database Setup
Create the required tables in SQL Server:

## Setup

1. **Clone the repository:**

   ```bash
   git clone https://github.com/yourusername/FlexTaskScheduler.git
   cd FlexTaskScheduler
