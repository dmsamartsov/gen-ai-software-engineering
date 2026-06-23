from mcp.server.fastmcp import FastMCP
import os
import json
import glob

# Create a FastMCP server
mcp = FastMCP("pipeline-status")

BASE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
RESULTS_DIR = os.path.join(BASE_DIR, "shared", "results")

@mcp.tool()
def get_transaction_status(transaction_id: str) -> str:
    """Gets the current status of a transaction from shared/results."""
    if not os.path.exists(RESULTS_DIR):
        return "Results directory not found."
    
    files = glob.glob(os.path.join(RESULTS_DIR, "*.json"))
    for f in files:
        with open(f, "r") as file:
            try:
                data = json.load(file)
                if data.get("data", {}).get("transaction_id") == transaction_id:
                    return f"Transaction {transaction_id} found. Status: {data['data'].get('status', 'unknown')}. Agent: {data.get('source_agent', 'unknown')}."
            except json.JSONDecodeError:
                pass
                
    return f"Transaction {transaction_id} not found in results."

@mcp.tool()
def list_pipeline_results() -> str:
    """Returns a summary of all processed transactions."""
    if not os.path.exists(RESULTS_DIR):
        return "Results directory not found."
        
    files = glob.glob(os.path.join(RESULTS_DIR, "*.json"))
    results = []
    
    for f in files:
        with open(f, "r") as file:
            try:
                data = json.load(file)
                tx_id = data.get("data", {}).get("transaction_id", "Unknown")
                status = data.get("data", {}).get("status", "unknown")
                results.append(f"{tx_id}: {status}")
            except json.JSONDecodeError:
                pass
                
    if not results:
        return "No transactions processed yet."
        
    return "\n".join(results)

@mcp.resource("pipeline://summary")
def get_summary() -> str:
    """Returns the latest pipeline run summary as text."""
    if not os.path.exists(RESULTS_DIR):
        return "Results directory not found. Pipeline has not run."
        
    files = glob.glob(os.path.join(RESULTS_DIR, "*.json"))
    cleared_count = 0
    rejected_count = 0
    settled_count = 0
    total = len(files)
    
    for f in files:
        with open(f, "r") as file:
            try:
                data = json.load(file)
                status = data.get("data", {}).get("status", "unknown")
                if status == "rejected":
                    rejected_count += 1
                elif status == "cleared":
                    cleared_count += 1
                elif status == "settled":
                    settled_count += 1
            except json.JSONDecodeError:
                pass
                
    return f"Pipeline Summary\nTotal Processed: {total}\nSettled: {settled_count}\nCleared: {cleared_count}\nRejected: {rejected_count}"

if __name__ == "__main__":
    mcp.run()
