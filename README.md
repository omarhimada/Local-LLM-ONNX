
# OLLM
- **Completely local** LLM chat desktop application that uses the *ONNX Generative AI Runtime*. 
- **Does not make any networking requests outside of the local machine.**
- **Zero HTTP** *(e.g.: API calls to OpenAI, Gemini)*, 
- **Zero REST API middle-layer** *(e.g.: GPT4All)* 
- **Zero WebSocket middle-layer** *(Ollama, LM Studio, etc.)*.
- Loads a local LLM model. 
- The latest release utilizes **gpt-oss-20b**.

![Reasoning example GIF](.Images/example.gif)
```mermaid
flowchart TD
    subgraph group_ui["WPF shell"]
      node_app["App shell<br/>WPF entry<br/>[App.xaml.cs]"]
      node_loading["Loading screen<br/>WPF window"]
      node_mainwin["Main chat<br/>WPF window<br/>[MainWindow.xaml.cs]"]
      node_thinking(("Thinking overlay<br/>UI adornment<br/>[FloatingAdorner.cs]"))
    end
    subgraph group_runtime["Conversation runtime"]
      node_initmodels["Model check<br/>startup validation"]
      node_linearcomm["Turn flow<br/>orchestrator"]
    end
    subgraph group_memory["Memory search"]
      node_discussion["Discussion store<br/>memory record<br/>[Discussion.cs]"]
      node_remember["Recall logic<br/>retrieval<br/>[Remember.cs]"]
      node_miniembed["Mini embedder<br/>embedding wrapper<br/>[MiniEmbedder.cs]"]
    end
    subgraph group_state["Runtime state"]
      node_modelstate["Model state<br/>runtime state<br/>[ModelState.cs]"]
      node_embedstate["Embed state<br/>runtime state<br/>[EmbedderState.cs]"]
      node_parserstate["Parser state<br/>incremental parse"]
    end
    subgraph group_models["Local models"]
      node_gptoss[("GPT-OSS<br/>LLM model")]
      node_embedmodel[("Embed model<br/>embedding model")]
      node_medgemma[("MedGemma<br/>LLM model")]
      node_sdpath[("SD assets<br/>diffusion model")]
    end
    subgraph group_image["Image generation"]
      node_diffusion["Diffusion<br/>image pipeline<br/>[Diffusion.cs]"]
      node_scheduler["LMS scheduler<br/>sampling scheduler<br/>[LmsScheduler.cs]"]
    end
    subgraph group_utils["Formatting and codegen"]
      node_mdfmt["Markdown fmt<br/>text formatting<br/>[Md.cs]"]
      node_syntax["Syntax tools<br/>highlighting<br/>[Highlighter.cs]"]
      node_j2cs["Jinja parser<br/>template compiler<br/>[Converter.cs]"]
    end
    node_app -->|"shows startup"| node_loading
    node_app -->|"validates models"| node_initmodels
    node_loading -->|"prepares"| node_modelstate
    node_loading -->|"prepares"| node_embedstate
    node_loading -->|"opens"| node_mainwin
    node_mainwin -->|"drives chat"| node_linearcomm
    node_mainwin -->|"shows status"| node_thinking
    node_linearcomm -->|"retrieves memory"| node_remember
    node_linearcomm -->|"uses chat model"| node_modelstate
    node_linearcomm -->|"tracks output"| node_parserstate
    node_remember -->|"queries"| node_discussion
    node_remember -->|"embeds query"| node_miniembed
    node_miniembed -->|"loads from disk"| node_embedmodel
    node_modelstate -->|"binds to"| node_gptoss
    node_embedstate -->|"binds to"| node_embedmodel
    node_modelstate -.->|"supports"| node_medgemma
    node_linearcomm -->|"prompts"| node_gptoss
    node_linearcomm -->|"formats response"| node_mdfmt
    node_mdfmt -->|"highlights code"| node_syntax
    node_gptoss -.->|"uses template"| node_j2cs
    node_diffusion -->|"loads assets"| node_sdpath
    node_diffusion -->|"uses scheduler"| node_scheduler
    node_mainwin -.->|"optional image flow"| node_diffusion
    click node_app "https://github.com/omarhimada/local-llm-onnx/blob/master/App.xaml.cs"
    click node_loading "https://github.com/omarhimada/local-llm-onnx/blob/master/LoadingWindow.xaml.cs"
    click node_mainwin "https://github.com/omarhimada/local-llm-onnx/blob/master/MainWindow.xaml.cs"
    click node_thinking "https://github.com/omarhimada/local-llm-onnx/blob/master/State/Thinking/FloatingAdorner.cs"
    click node_initmodels "https://github.com/omarhimada/local-llm-onnx/blob/master/Initialization/EnsureModelsArePresent.cs"
    click node_linearcomm "https://github.com/omarhimada/local-llm-onnx/blob/master/Interact/LinearCommunication.cs"
    click node_discussion "https://github.com/omarhimada/local-llm-onnx/blob/master/Memory/Discussion.cs"
    click node_remember "https://github.com/omarhimada/local-llm-onnx/blob/master/Memory/Remember.cs"
    click node_miniembed "https://github.com/omarhimada/local-llm-onnx/blob/master/Memory/MiniEmbedder.cs"
    click node_modelstate "https://github.com/omarhimada/local-llm-onnx/blob/master/State/ModelState.cs"
    click node_embedstate "https://github.com/omarhimada/local-llm-onnx/blob/master/State/EmbedderState.cs"
    click node_parserstate "https://github.com/omarhimada/local-llm-onnx/blob/master/State/IncrementalParserState.cs"
    click node_gptoss "https://github.com/omarhimada/local-llm-onnx/tree/master/ONNX/gpt-oss-20b"
    click node_embedmodel "https://github.com/omarhimada/local-llm-onnx/tree/master/ONNX/Embed/all-MiniLM-L6-v2-onnx"
    click node_medgemma "https://github.com/omarhimada/local-llm-onnx/tree/master/ONNX/med-gemma-27b"
    click node_sdpath "https://github.com/omarhimada/local-llm-onnx/tree/master/ONNX/SD/0nnX00Aammnpdxebr"
    click node_diffusion "https://github.com/omarhimada/local-llm-onnx/blob/master/SD/Diffusion.cs"
    click node_scheduler "https://github.com/omarhimada/local-llm-onnx/blob/master/SD/LmsScheduler.cs"
    click node_mdfmt "https://github.com/omarhimada/local-llm-onnx/blob/master/Utility/Md.cs"
    click node_syntax "https://github.com/omarhimada/local-llm-onnx/blob/master/Utility/Syntax/Highlighter.cs"
    click node_j2cs "https://github.com/omarhimada/local-llm-onnx/blob/master/Utility/J2CS/Converter.cs"
    classDef toneNeutral fill:#f8fafc,stroke:#334155,stroke-width:1.5px,color:#0f172a
    classDef toneBlue fill:#dbeafe,stroke:#2563eb,stroke-width:1.5px,color:#172554
    classDef toneAmber fill:#fef3c7,stroke:#d97706,stroke-width:1.5px,color:#78350f
    classDef toneMint fill:#dcfce7,stroke:#16a34a,stroke-width:1.5px,color:#14532d
    classDef toneRose fill:#ffe4e6,stroke:#e11d48,stroke-width:1.5px,color:#881337
    classDef toneIndigo fill:#e0e7ff,stroke:#4f46e5,stroke-width:1.5px,color:#312e81
    classDef toneTeal fill:#ccfbf1,stroke:#0f766e,stroke-width:1.5px,color:#134e4a
    class node_app,node_loading,node_mainwin,node_thinking,node_mdfmt,node_syntax,node_j2cs toneBlue
    class node_initmodels,node_linearcomm toneAmber
    class node_discussion,node_remember,node_miniembed toneMint
    class node_modelstate,node_embedstate,node_parserstate toneRose
    class node_gptoss,node_embedmodel,node_medgemma,node_sdpath toneIndigo
    class node_diffusion,node_scheduler toneTeal
```
## Roadmap 
- **High Priority**
    1. Memory/conversation state management with retrieval augmentation and chat histories. **90% complete**
        - Initializes a local SQLite database if it does not exist.
  		- Utilize `VectorData` abstractions and connectors for SQLite.
      		- Microsoft is sort of developing solutions in parallel regarding native SQL Vector storage *(i.e.: `Microsoft.SemanticKernel.Connectors.SqliteVec` pre-release)*
      	-  Implemented two methods:
            1. `MemorizeDiscussion(...) // Store a discussion that had occurred.`
            2. `RememberDiscussions(...) // Try to remember before responding`
          - `VectorSearch` occurs with decay parameters like `halfLifeDays = 365, etc.`
          - **The goal is that they keep learning** and you **backup the local database yourself**. *(i.e.: the model lives in this one machine and learns forever.*)
        
- **Low Priority**
  - Other planned QOL improvements (low priority):
    - Image/vision -> embeddings -> retrieval augmentation. I don't want to fast-forward this with existing solutions.
    - Changing models via dropdown menu selection

### Setup
- Your directory setup should look something like the diagram below, although the `model.onnx` and `model.onnx_data` will be absent. This is due to size (gigabytes).
```
        ,______________________________________________________
        | OnnxLocalLLM\ONNX\gpt-oss-20b
        |
        | model.onnx        <------------------ Download this 
        | model.onnx_data   <------------------ Download this 
        |
        | genai_config.json
        | special_tokens_map.json
        | tokenizer_config.json
        | tokenizer.json
        | vocab.json
        |____________________________________________________
```
