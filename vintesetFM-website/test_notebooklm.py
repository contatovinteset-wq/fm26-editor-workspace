import asyncio
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

async def main():
    server_params = StdioServerParameters(
        command="C:\\Users\\Raphael\\AppData\\Local\\Programs\\Python\\Python313\\Scripts\\notebooklm-mcp-server.exe",
        args=[],
    )
    try:
        async with stdio_client(server_params) as (read, write):
            async with ClientSession(read, write) as session:
                await session.initialize()
                result = await session.call_tool("notebooklm_list_notebooks", {})
                if not result:
                     result = await session.call_tool("list_notebooks", {}) # fallback if named differently
                
                print("====================================")
                print("SEUS CADERNOS DO NOTEBOOKLM:")
                print("====================================")
                for content in getattr(result, 'content', []):
                    if hasattr(content, 'text'):
                        print(content.text)
                    else:
                        print(content)
                        
    except Exception as e:
        print(f"Erro ao listar notebooks: {e}")

asyncio.run(main())
