# Hashpool REST API

> *This is an Experimental Project, the creators make no assertion of future maintanance or support.  Patch releases will likely **not** be backwards compatible until stability in design and features is achieved.*

The Hashpool provides a REST endpoint for submitting raw (possibly partially) signed protobuf HAPI transactions which are then temporarily held in state and submitted to the appropriate Hedera Gossip Node via native gRPC protocol at the proper submission time.

It also provides methods to add signatures to pending transactions, facilitating the orchestration of signatures by multiple parties supporting transactions that are not yet enabled as _scheduled transactions_ by the Hedera network.

## How it works

When deployed, this software exposes a REST API endpoint that accepts HAPI protobuf encoded transactions as POST requests.  Upon receiving a POST request containing the encoded bytes representing a `SignedTransaction` structure; it decodes this structure, examining the transaction itself and enumerating the signatures included in the data.  If the system recognizes the transaction as valid, it then determines if the transaction’s start time requires immediate forwarding to the appropriate Hedera gossip node; or if the transaction should be held in memory until such time the transaction is in the valid window of submission.  For transactions held in memory waiting for their start times, external parties can call additional methods to add signatures to existing transactions.  In this way it is possible to orchestrate the signing of arbitrary Hedera transactions by multiple parties.  

### Endpoint Operations
| Endpoint | Description |
| -------- | ----------- |
| /Info |	Retrieves information about this instance of the Hashpool, including the addresses of known Hedera gossip gRPC nodes that it is capable of submitting transactions to. |
| /Transactions |	Retrieves the list of pending transactions (GET) or accepts new pending transactions (POST) for processing and possibly temporary holding before submitting to the proper Hedera gossip node. |
| /Transactions/{id}/receipt | Asks the Hedera network for the receipt for a given transaction (GET), assuming it has been submitted. |
| /Transactions/{id}/protobuf	| Retrieves (GET) the serialized protobuf representation of a given transaction.  Wallets can retrieve the transaction, examine it, sign it, and then add their signature to the pending transaction. |
| /Transactions/{id}/signatures |	Adds a signature (POST) to a pending tansaction. |


**Please note:**  Since this project is a work in progress and a naive implementation, it can be attacked in many ways, including signature poisoning (deliberate attempts to provide invalid signatures) and denial of service attacks.  Future work will include mitigation of these vulnerabilities.  Additionally, it provides no persistence across restarts, all information is held in memory, a future version may be written that additionally relies on storage allowing pending transaction information to survive restarts.

## Quick Start

This project and its components are written for the .NET 7.0 environment.
1.  git clone https://github.com/the-creators-galaxy/hashpool.git
2.  cd hashpool/src/HashpoolApi
3.	dotnet restore
4.	dotnet run
5.	Navigate a browser to one of the URLs identified in the output appended with `/swagger` to access the project’s OpenAPI user interface.

The result is a Haspool instance connected to Hedera’s public _testnet_ mirror, and thusly will submit transactions to the Hedera _testnet_.

For production deployments, the `Mirror` environment variable can be set in the host environment to direct software to support the desired network.  Please see the `appsettings.json` file for more information regarding runtime configuration.

## Testing

The project contains a limited number of integration tests, please see the `test/HashpoolTest` project for more information.  The tests can be run from the command line or from Visual Studio 2022 test explorer.


## Related Software

The following related JavaScript NPM packages are also under co-devlopment and are designed to aid in the creation of hedera transactions and interaction with instances of this hashpool and a standard hedera mirror node:


* [@bugbytes/hapi-hashpool](https://www.npmjs.com/package/@bugbytes/hapi-hashpool)  

  Provides a helper client for interacting with Hashpool REST server.

* [@bugbytes/hapi-proto](https://www.npmjs.com/package/@bugbytes/hapi-proto)

  Publishes typescript definitions for the entirety of the 
  [Hedera API Protobuf](https://github.com/hashgraph/hedera-protobufs) specification.  

* [@bugbytes/hapi-util](https://www.npmjs.com/package/@bugbytes/hapi-util)  

  Adds helper functions to aid in the common tasks of converting well known 
  string formats to HAPI protobuf objects and back again.

* [@bugbytes/hapi-mirror](https://www.npmjs.com/package/@bugbytes/hapi-mirror)

  Provides a helper client for retrieving data from an Hedera Mirror Node.

* [@bugbytes/hapi-connect](https://www.npmjs.com/package/@bugbytes/hapi-connect)  

  An independent implementation of the server/website side of the 
  *HashConnect* protocol.

