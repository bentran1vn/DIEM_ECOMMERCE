# DIEM E-commerce Architecture

```mermaid
flowchart TD
    subgraph Client
        WebBrowser[Web Browser]
    end

    subgraph Frontend
        RazorPages[Razor Pages]
        React[React Components]
    end
    
    subgraph APIServer[API Server]
        APIs[API Endpoints]
        Services[Business Services]
    end
    
    subgraph DataLayer[Data Layer]
        MasterDB[(Master DB)]
        SlaveDB[(Slave DB)]
        Redis[(Redis Cache)]
        
        MasterDB -- Replication --> SlaveDB
    end
    
    WebBrowser --> Frontend
    RazorPages -- API Calls --> APIs
    React -- API Calls --> APIs
    
    APIs --> Services
    
    Services -- Read Data --> SlaveDB
    Services -- Read Cache --> Redis
    Services -- Write Data --> MasterDB