# Blend's Little Import Tool (BLIT)

Blit is a content migration utility by Blend Interactive which is designed to help eliminate some common problems while loading imported content in Optimizely CMS. Specifically:

* Idempotence - Multiple runs of the same import will not duplicate content, make unnecessary changes, or update fields it's not specifically configured to update.
* File-based import - Blit imports from specially designed files. To change what gets imported, one changes the *files*, not *code*. You can fix import problems by updating XML files, rather than a lengthy code deployment process.
* Resumability - Blit tracks progress and resumes where it left off in the case of interrupted imports.
* Environmental independence - Blit can paper over some small environmental differences by *variables*, which are set on a per-instance basis and can be referenced in imported content. Variables are then resolved at import time in each environment.
* Link resolution - Blit can find and resolve internal links into permanent URLs, even for content that doesn't exist yet (see next point).
* Two-stage import - Blit first imports the bare-bones necessary for content to exist, then runs a second pass in which any properties that may depend on other imported content are resolved.

**Blit is currently in beta and only supports Optimizely CMS version 12.**

## How it works

Content migration is often taken in 3 steps:

* Extract — Migrating the content out of the current system.
* Transform — Manipulating the content into a shape the new system can understand.
* Load — Place the manipulated content into the destination system.

Blit is only concerned with the `Load` step. At Blit's core is a content format that enables several useful features:

* The format has a query syntax that allows content to be found. This is key for a couple of reasons:
    * Content can "find itself" in the system, meaning if that content already exists, it can be updated, otherwise it can be created, making imports idempotent.
    * Content can find its parent node. This means you don’t have to hard-code IDs. Imported content can even be the child of other imported content, as long as the parent node is first in the import process.
* Properties are imported in two distinct stages:
    *  The Initial Stage handles creating/publishing all content nodes and fills in any properties that (you have determined) have no dependencies on other content.
    *  The Deferred Stage then fills in any properties that might refer to other imported content, such as content references, XHtmlString links, etc.
    *  This solves the chicken-or-the-egg paradox by ensuring all imported content exists before attempting to resolve links between those pieces of content.
* The format has a fragmented text format that allows you to mix HTML, variables, and references to other content, either as queries for existing content, or new content directly embedded in the fragment stream. For example, you can embed image data directly into your XhtmlString property, and it will be imported into the "For this page" container and referenced correctly when imported.

For a full introduction, see our blog post [Introducing Blend's Little Import Tool](https://www.blendinteractive.com/thoughts/introducing-blends-little-import-tool/)

## Installation

Install the following packages:

* `BlendInteractive.Blit` - Contains the core definitions and abstractions used by the BLIT format.
* `BlendInteractive.Blit.Optimizely` - Contains Optimizely-specific implementation details, including the import scheduled job.
* `BlendInteractive.Blit.Optimizely.UI` - Contains the Optimizely admin UI.

## Usage

(More documentation on this to come)

1. Scrape or otherwise transform your source content into Blit XML files (see the XML format documentation below).
2. Deploy your XML files and an index file (with each XLM file listed, one per line) into a location the server can read them (typically S3 or Azure Storage, anywhere accessible via HTTP or HTTPS)
3. From the Blit admin area, queue the path or URL to your index file.
4. Run the `Blit Processing Job` scheduled job to process the queue.

## BLIT Content Concepts and XML Format

Below is an explanation of Blit's core concepts and XML format. For an example of generating this file (with some context and explanation on each part), see the [example program](examples/ExampleGenerator/Program.cs).

### Variables

Variables are a way of taking values that vary between different instances of the CMS out of the content XML and scoping it to either the CMS instance, or to a specific import. Things such as page IDs that may vary between one environment and another can be referenced as a variable allowing the same content files to be used in both environments despite the differences. Variables are managed via the Blit admin area.

### Queries

Queries search for existing content and are used both to enable content to find itself, but also to allow content to link to other existing content.

In XML, queries are part of other structures and have different root nodes depending on context, but within that node you can have any number of locator nodes.

Queries are made up of any number of "locators":

* Match Text Locator - Locates content where a property matches a value (using string comparison).
  * XML: text element
    * @name attribute for the name of the property.
    * Child elements resolve to the value to search for.
* Of Type Locator - Locates content of a particular content type.
  * XML: type element
    * Child text that resolves to the type (the Full Name of the C# class that represents the content type)
* Match ID Locator - Locates content with a particular ID.
  * XML: id element
    * Child text that resolves to the ID to match
* Tree Locator - Locates content that is either a direct child, or descendant of a subquery.
  * XML: tree element
    * @type attribute with either "Ancestor" or "Child"
    * Child elements are locator nodes that define the query to find the parent or ancestor node.
* For this Page Locator - Locates content that is in the "For this Page" section of a page. This only works for locating directly embedded content.
  * XML: forthispage element
* For this Site Locator - Locates content that is in the "For this Site" section of a site.
 * XML: forthissite element
    * Child text that resolves to the Site ID of the site’s global section to use

### Content References

References can be represented two ways:

* Content Query - A Query to find existing content. 
* Embedded Content - Content that is embedded directly into the content file. All content files have one Embedded Content node at the root, simply known as "content".

### Embedded content

Embedded content represents a piece of content within the CMS, and is made up of the following:

* Id - A unique identifier which can be any arbitrary string.
  * XML: @id attribute
* Type - The content type (the C# type name)
  * XML: @type attribute
* Content Action - A list of actions to take on this content.
  * In XML:
    * actions Element
    * Child nodes of any of these elements: create, update, move, delete
  * These actions are available:
    * None - Do nothing.
    * Create - Create this content if it does not exist
    * Update - Update this content if it already exists and the values embedded in the content file differ from the values in the CMS.
    * Move - If the content is not a child of the correct parent node, move this content to be a child of the correct parent node. (Currently unsupported)
    * Delete - If found, delete this piece of content (Currently unsupported)
* Self Query - A content query to find this piece of content if it exists in the CMS. This is required to prevent duplicates.
  * XML: query element
* Parent Query - A content query to find the parent node under which this content should exist. This is required to ensure the content is inserted into the correct place in the content tree.
  * XML: parent element
* Stage One - A collection of properties and values. This collection should contain everything necessary for the Self Query to succeed. This collection should not contain any property values that might reference other content.
  * XML: stageone element
    * Child nodes are a collection of property nodes
* Stage Two - An optional second stage which updates this content in the CMS with any properties that might potentially reference newly created content from this import. This ensures that all content has at least been created, so values in Stage Two can reference that content.
  * XML: stagetwo element
    * Child nodes are a collection of property nodes

### Properties

Content is made up of properties, all of which are represented as a stream of text "fragments" in the XML. These are the supported property types:

* Text Property - Text Property is the most basic property type and can represent any value that can be represented as a string. For example, a Text Property may contain HTML, integers, dates, etc. The import process takes care of conversions to the appropriate data type for the CMS.
  * XML: text element
    * @name attribute 
    * All child nodes (text and elements) make up fragments that resolve to the value to match
* Blob Property - Represents a collection of binary data. This is used for media assets.
  * XML: blob element
    * @name attribute
    * @extension attribute
    * Child text that resolves to the base64 encoded binary of the file contents
* Nested Property - Represents when a content type has another content type embedded as a property of the parent type. For example, a Homepage may have a Seo Data content type as a property of the Homepage. This allows "local block" nesting.
  * XML: nested element
    * @name attribute
    * Child elements of properties of that nested content type
* List Property - A collection of property values. This can be used to represent a collection of Categories, a Content Area (as a list of content references), or even an IList<> of blocks.
  * XML: list element
    * @name attribute
    * Child item elements, each with child text fragments that resolve to the values of the list.

### Fragments

All property values are represented as a stream of Fragments which are resolved at import-time to final values. The following fragment types are supported:

* Text Fragment - The most basic fragment type. This represents a string and is a simple text node, or CDATA node in XML as necessary.
* Inline Content - Represents some content directly embedded in the content file. This embedded content can end up stored anywhere in the CMS tree, but the source of its data is embedded directly in this property. This is most frequently used to represent inline blocks or media.
  * XML:
    * content element
    * @embedtype attribute, which can be ID, PermanentUrl, or EmbeddedBlock
    * Child nodes represent the Embedded Content.
  * This can be embedded in three ways:
    * ID - Used for content areas or other lists that expect content IDs
    * Permanent Url - Returns the CMS’s internal format for the content’s URL. Used for embedded links in HTML that the CMS will recognize as internal site links.
    * Embedded Block - Embeds the content reference as a block. Used to embed a block in an XHtmlString property.
* Content Lookup - Represents a query to some other existing content, either content that can be reasonably expected to exist in the content tree, or content that will be created as part of the import. Content Lookup values also include a fallback. 
  * XML: lookup element
    * @fallback attribute
    * @embedtype attribute
    * Child nodes represent the Query to look up this content
  * Like Inline Content, the resolved reference can be embedded in three ways:
    * ID - Used for content areas or other lists that expect content IDs
    * Permanent Url - Returns the CMS’s internal format for the content’s URL. Used for embedded links in HTML that the CMS will recognize as internal site links.
    * Embedded Block - Embeds the content reference as a block. Used to embed a block in an XHtmlString property.
* Variable Reference - Resolves a variable value.
  * XML: var element
    * @name attribute
* Category Path - A list of Category name nodes. Used to ensure categories exist and are applied to a piece of content.
  * XML: categorypath element
    * Child nodes are a collection of category elements, each element containing text that resolves to the name of a category. The elements are listed in hierarchical order, with the root category name first, and the category name to associate to the content last.

## Limitations and Known Issues

* There's an issue with the side menu rendering in the admin area. The side menu is not necessary, but it looks weird.
* Media is supported, but should only be used with smaller files, as the binary data is base64-encoded and inserted into the content XML files. Big files will make for huge XML.
* Blit does not support inline-blocks. Only shared blocks, or blocks used as properties within a content type.

## Potential or Planned Future Improvements

* Ability to reference binary data via URL or file path, rather than base64 encoded/embedding
* Support for inline-blocks
* Support for SaaS as a target via GraphQL and content APIs
