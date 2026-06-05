--
-- PostgreSQL database dump
--

-- Dumped from database version 17.4
-- Dumped by pg_dump version 17.4

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

ALTER TABLE IF EXISTS ONLY public.token_usage DROP CONSTRAINT IF EXISTS fk_token_usage_conversation_id;
ALTER TABLE IF EXISTS ONLY public.project_note DROP CONSTRAINT IF EXISTS fk_project_note_project_id;
ALTER TABLE IF EXISTS ONLY public.project_decision DROP CONSTRAINT IF EXISTS fk_project_decision_project_id;
ALTER TABLE IF EXISTS ONLY public.pipeline_stage DROP CONSTRAINT IF EXISTS fk_pipeline_stage_project_id;
ALTER TABLE IF EXISTS ONLY public.parking_lot_item DROP CONSTRAINT IF EXISTS fk_parking_lot_item_conversation_id;
ALTER TABLE IF EXISTS ONLY public.message DROP CONSTRAINT IF EXISTS fk_message_conversation_id;
ALTER TABLE IF EXISTS ONLY public.conversation DROP CONSTRAINT IF EXISTS fk_conversation_stage_id;
ALTER TABLE IF EXISTS ONLY public.artefact DROP CONSTRAINT IF EXISTS fk_artefact_project_id;
DROP INDEX IF EXISTS public.idx_uq_project_code;
DROP INDEX IF EXISTS public.idx_token_usage_conversation_id;
DROP INDEX IF EXISTS public.idx_project_status;
DROP INDEX IF EXISTS public.idx_project_note_project_id;
DROP INDEX IF EXISTS public.idx_project_decision_project_id;
DROP INDEX IF EXISTS public.idx_pipeline_stage_project_id_sort_order;
DROP INDEX IF EXISTS public.idx_pipeline_stage_project_id;
DROP INDEX IF EXISTS public.idx_parking_lot_item_status;
DROP INDEX IF EXISTS public.idx_parking_lot_item_conversation_id;
DROP INDEX IF EXISTS public.idx_message_created_at;
DROP INDEX IF EXISTS public.idx_message_conversation_id;
DROP INDEX IF EXISTS public.idx_conversation_stage_id;
DROP INDEX IF EXISTS public.idx_artefact_project_id;
DROP INDEX IF EXISTS public.idx_artefact_project_filepath;
DROP INDEX IF EXISTS public.flyway_schema_history_s_idx;
ALTER TABLE IF EXISTS ONLY public.token_usage DROP CONSTRAINT IF EXISTS pk_token_usage;
ALTER TABLE IF EXISTS ONLY public.project_note DROP CONSTRAINT IF EXISTS pk_project_note;
ALTER TABLE IF EXISTS ONLY public.project_decision DROP CONSTRAINT IF EXISTS pk_project_decision;
ALTER TABLE IF EXISTS ONLY public.project DROP CONSTRAINT IF EXISTS pk_project;
ALTER TABLE IF EXISTS ONLY public.pipeline_stage DROP CONSTRAINT IF EXISTS pk_pipeline_stage;
ALTER TABLE IF EXISTS ONLY public.parking_lot_item DROP CONSTRAINT IF EXISTS pk_parking_lot_item;
ALTER TABLE IF EXISTS ONLY public.message DROP CONSTRAINT IF EXISTS pk_message;
ALTER TABLE IF EXISTS ONLY public.conversation DROP CONSTRAINT IF EXISTS pk_conversation;
ALTER TABLE IF EXISTS ONLY public.artefact DROP CONSTRAINT IF EXISTS pk_artefact;
ALTER TABLE IF EXISTS ONLY public.flyway_schema_history DROP CONSTRAINT IF EXISTS flyway_schema_history_pk;
DROP TABLE IF EXISTS public.token_usage;
DROP TABLE IF EXISTS public.project_note;
DROP TABLE IF EXISTS public.project_decision;
DROP TABLE IF EXISTS public.project;
DROP TABLE IF EXISTS public.pipeline_stage;
DROP TABLE IF EXISTS public.parking_lot_item;
DROP TABLE IF EXISTS public.message;
DROP TABLE IF EXISTS public.flyway_schema_history;
DROP TABLE IF EXISTS public.conversation;
DROP TABLE IF EXISTS public.artefact;
DROP TYPE IF EXISTS public.stage_type;
DROP TYPE IF EXISTS public.project_status;
DROP TYPE IF EXISTS public.pipeline_stage_status;
DROP TYPE IF EXISTS public.parking_lot_status;
DROP TYPE IF EXISTS public.parking_lot_priority;
DROP TYPE IF EXISTS public.message_role;
DROP TYPE IF EXISTS public.conversation_status;
DROP TYPE IF EXISTS public.compliance_domain;
DROP EXTENSION IF EXISTS "uuid-ossp";
--
-- Name: uuid-ossp; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA public;


--
-- Name: EXTENSION "uuid-ossp"; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION "uuid-ossp" IS 'generate universally unique identifiers (UUIDs)';


--
-- Name: compliance_domain; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.compliance_domain AS ENUM (
    'clinical_uk',
    'generic',
    'finance'
);


ALTER TYPE public.compliance_domain OWNER TO postgres;

--
-- Name: conversation_status; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.conversation_status AS ENUM (
    'active',
    'paused',
    'completed'
);


ALTER TYPE public.conversation_status OWNER TO postgres;

--
-- Name: message_role; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.message_role AS ENUM (
    'user',
    'assistant',
    'system'
);


ALTER TYPE public.message_role OWNER TO postgres;

--
-- Name: parking_lot_priority; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.parking_lot_priority AS ENUM (
    'critical',
    'high',
    'medium'
);


ALTER TYPE public.parking_lot_priority OWNER TO postgres;

--
-- Name: parking_lot_status; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.parking_lot_status AS ENUM (
    'open',
    'resolved',
    'deferred'
);


ALTER TYPE public.parking_lot_status OWNER TO postgres;

--
-- Name: pipeline_stage_status; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.pipeline_stage_status AS ENUM (
    'not_started',
    'in_progress',
    'complete',
    'blocked'
);


ALTER TYPE public.pipeline_stage_status OWNER TO postgres;

--
-- Name: project_status; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.project_status AS ENUM (
    'discovery',
    'in_progress',
    'complete',
    'archived'
);


ALTER TYPE public.project_status OWNER TO postgres;

--
-- Name: stage_type; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.stage_type AS ENUM (
    'requirements_discovery',
    'architecture',
    'design',
    'pxd',
    'clinical_safety',
    'planning',
    'normalisation',
    'prototype'
);


ALTER TYPE public.stage_type OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: artefact; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.artefact (
    artefact_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    project_id uuid NOT NULL,
    version integer DEFAULT 1 NOT NULL,
    file_path character varying(500) NOT NULL,
    s3_key character varying(1000) NOT NULL,
    content_type character varying(100) NOT NULL,
    size_bytes bigint,
    created_by character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.artefact OWNER TO postgres;

--
-- Name: conversation; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.conversation (
    conversation_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    stage_id uuid NOT NULL,
    status public.conversation_status DEFAULT 'active'::public.conversation_status NOT NULL,
    message_count integer DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    resumed_at timestamp with time zone,
    current_phase integer DEFAULT 0 NOT NULL,
    phase_name character varying(100) DEFAULT 'mode_selection'::character varying NOT NULL,
    total_phases integer DEFAULT 12 NOT NULL,
    questions_asked integer DEFAULT 0 NOT NULL,
    estimated_total_questions integer,
    requirements_captured integer DEFAULT 0 NOT NULL
);


ALTER TABLE public.conversation OWNER TO postgres;

--
-- Name: flyway_schema_history; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.flyway_schema_history (
    installed_rank integer NOT NULL,
    version character varying(50),
    description character varying(200) NOT NULL,
    type character varying(20) NOT NULL,
    script character varying(1000) NOT NULL,
    checksum integer,
    installed_by character varying(100) NOT NULL,
    installed_on timestamp without time zone DEFAULT now() NOT NULL,
    execution_time integer NOT NULL,
    success boolean NOT NULL
);


ALTER TABLE public.flyway_schema_history OWNER TO postgres;

--
-- Name: message; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.message (
    message_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    conversation_id uuid NOT NULL,
    role public.message_role NOT NULL,
    content text NOT NULL,
    token_count integer,
    user_ern character varying(200),
    given_name character varying(100),
    family_name character varying(100),
    images jsonb,
    documents jsonb,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.message OWNER TO postgres;

--
-- Name: COLUMN message.images; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.message.images IS 'Optional JSONB array of image attachments: [{data: base64, mediaType: string}]';


--
-- Name: COLUMN message.documents; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.message.documents IS 'Optional JSONB array of document attachments: [{data: base64, mediaType: string, fileName: string}]';


--
-- Name: parking_lot_item; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.parking_lot_item (
    parking_lot_item_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    conversation_id uuid NOT NULL,
    content text NOT NULL,
    priority public.parking_lot_priority DEFAULT 'medium'::public.parking_lot_priority NOT NULL,
    status public.parking_lot_status DEFAULT 'open'::public.parking_lot_status NOT NULL,
    source_phase integer NOT NULL,
    resolved_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.parking_lot_item OWNER TO postgres;

--
-- Name: pipeline_stage; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.pipeline_stage (
    pipeline_stage_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    project_id uuid NOT NULL,
    stage_type public.stage_type NOT NULL,
    status public.pipeline_stage_status DEFAULT 'not_started'::public.pipeline_stage_status NOT NULL,
    iteration integer DEFAULT 1 NOT NULL,
    started_at timestamp with time zone,
    completed_at timestamp with time zone,
    completed_by character varying,
    sort_order smallint DEFAULT 0 NOT NULL
);


ALTER TABLE public.pipeline_stage OWNER TO postgres;

--
-- Name: project; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.project (
    project_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    code character varying(10) NOT NULL,
    name character varying(200) NOT NULL,
    description character varying(2000),
    compliance_domain public.compliance_domain NOT NULL,
    status public.project_status DEFAULT 'discovery'::public.project_status NOT NULL,
    created_by character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    is_deleted boolean DEFAULT false NOT NULL,
    time_sheet_code character varying(50) NOT NULL
);


ALTER TABLE public.project OWNER TO postgres;

--
-- Name: project_decision; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.project_decision (
    project_decision_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    project_id uuid NOT NULL,
    title character varying(200) NOT NULL,
    context text NOT NULL,
    decision text NOT NULL,
    consequences text NOT NULL,
    author_ern character varying(200),
    author_given_name character varying(100),
    author_family_name character varying(100),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.project_decision OWNER TO postgres;

--
-- Name: project_note; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.project_note (
    project_note_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    project_id uuid NOT NULL,
    content text NOT NULL,
    author_ern character varying(200),
    author_given_name character varying(100),
    author_family_name character varying(100),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.project_note OWNER TO postgres;

--
-- Name: token_usage; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.token_usage (
    token_usage_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    conversation_id uuid NOT NULL,
    input_tokens integer NOT NULL,
    output_tokens integer NOT NULL,
    cache_read_input_tokens integer DEFAULT 0 NOT NULL,
    cache_write_input_tokens integer DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.token_usage OWNER TO postgres;

--
-- Data for Name: artefact; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.artefact (artefact_id, project_id, version, file_path, s3_key, content_type, size_bytes, created_by, created_at) FROM stdin;
6bbbd6c8-d04c-48ea-8d34-44ea632b9af2	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	1	feedback/ITERATION_REPORT_P01_i1.md	projects/03735ad1-8759-414e-a93f-ce8cc7bfc1fc/artefacts/feedback/ITERATION_REPORT_P01_i1.md/v1	text/markdown	7050	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	2026-05-31 19:47:50.916603+00
b73ada95-280e-47d2-9416-ce154f3f14f2	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	3	manifest.md	projects/03735ad1-8759-414e-a93f-ce8cc7bfc1fc/artefacts/manifest.md/v3	text/markdown	11933	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	2026-05-31 19:47:12.661328+00
e1607276-345b-4386-a5b0-43a7c63444b1	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	1	prototype/index.html	projects/03735ad1-8759-414e-a93f-ce8cc7bfc1fc/artefacts/prototype/index.html/v1	text/markdown	78285	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	2026-05-31 19:54:57.514179+00
f2b89727-1d2a-491c-980e-6fbe54e12f0e	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	2	requirements/REQ-001.md	projects/03735ad1-8759-414e-a93f-ce8cc7bfc1fc/artefacts/requirements/REQ-001.md/v2	text/markdown	16350	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	2026-05-31 19:46:02.635573+00
a07bd1a8-040a-4055-814c-98a703b56a7a	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	1	requirements/REQ-002.md	projects/03735ad1-8759-414e-a93f-ce8cc7bfc1fc/artefacts/requirements/REQ-002.md/v1	text/markdown	11726	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	2026-05-31 19:31:08.019417+00
b13d3794-5969-4b63-a3af-16318967328d	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	1	requirements/REQ-003.md	projects/03735ad1-8759-414e-a93f-ce8cc7bfc1fc/artefacts/requirements/REQ-003.md/v1	text/markdown	8320	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	2026-05-31 19:33:08.16758+00
10ca2a05-4c45-49cb-bde9-bfc75c40aaa4	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	1	requirements/REQ-004.md	projects/03735ad1-8759-414e-a93f-ce8cc7bfc1fc/artefacts/requirements/REQ-004.md/v1	text/markdown	6622	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	2026-05-31 19:35:29.171364+00
7e0e27e4-f4ff-4435-942a-11df96592c86	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	1	requirements/REQ-005.md	projects/03735ad1-8759-414e-a93f-ce8cc7bfc1fc/artefacts/requirements/REQ-005.md/v1	text/markdown	9028	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	2026-05-31 19:37:16.000289+00
55a5c2d3-572b-4563-92d0-1e4f94e79b90	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	1	requirements/REQ-006.md	projects/03735ad1-8759-414e-a93f-ce8cc7bfc1fc/artefacts/requirements/REQ-006.md/v1	text/markdown	9187	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	2026-05-31 19:43:19.099695+00
\.


--
-- Data for Name: conversation; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.conversation (conversation_id, stage_id, status, message_count, created_at, resumed_at, current_phase, phase_name, total_phases, questions_asked, estimated_total_questions, requirements_captured) FROM stdin;
c91b73d1-cbc4-42f7-979a-57288d51a9aa	e6655423-b83f-43ed-9b4f-6ad819676c30	active	136	2026-05-31 18:40:48.764623+00	\N	12	feedback_collection_and_evaluation_report	12	66	70	6
71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	007576f9-688f-4919-b276-9fb95537768d	active	8	2026-05-31 19:48:50.8138+00	\N	3	build_prototype	5	5	5	5
\.


--
-- Data for Name: flyway_schema_history; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.flyway_schema_history (installed_rank, version, description, type, script, checksum, installed_by, installed_on, execution_time, success) FROM stdin;
1	1	initial schema	SQL	V1__initial_schema.sql	373346166	postgres	2026-06-04 13:50:40.818698	21	t
2	2	artefact content to s3	SQL	V2__artefact_content_to_s3.sql	-1037442650	postgres	2026-06-04 13:50:40.873651	4	t
3	3	project code unique excludes deleted	SQL	V3__project_code_unique_excludes_deleted.sql	-320728458	postgres	2026-06-04 13:50:40.890689	2	t
4	4	add notes and decisions	SQL	V4__add_notes_and_decisions.sql	-34770285	postgres	2026-06-04 13:50:40.897502	4	t
5	5	add project time sheet code	SQL	V5__add_project_time_sheet_code.sql	823322299	postgres	2026-06-04 13:50:40.906467	1	t
\.


--
-- Data for Name: message; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.message (message_id, conversation_id, role, content, token_count, user_ern, given_name, family_name, images, documents, created_at) FROM stdin;
bb020300-03ef-45e4-a52a-009b4f4ed793	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	Please begin.	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:40:48.919609+00
cd99330a-623f-4505-bc5f-de9019846082	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Welcome to Healthcare Requirements Discovery!\nI'm here to help you create comprehensive, deterministic requirements that coding agents can execute.\n**How this works:**\n- You'll get individual requirement files (one per feature, not one massive document)\n- Each requirement includes an Evaluation Function Specification (deterministic pass/fail criteria)\n- A master blueprint (manifest.md) captures global standards and cross-cutting concerns\n**First, let's set the right level of detail for your needs.**\n---\n**🏃 QUICK MODE** (15–20 questions, ~30 minutes)\n• 3–5 core requirements • Basic eval specs • Best for: MVPs, rapid validation\n**⚖️ STANDARD MODE** (30–40 questions, ~60 minutes)\n• 10–15 prioritised requirements • Complete eval specs with 5–10 checks each • Full four-dimensional analysis • Best for: Most projects\n**📚 COMPREHENSIVE MODE** (50+ questions, 2+ hours)\n• 15–25 requirements • Exhaustive eval specs • Multiple personas • Best for: Regulated products, audit-ready documentation\n**🎛️ CUSTOM MODE**\n• You choose which phases to include/skip • Set question limits per phase\n---\n💬 **Which mode would work best for you?**\nSay "Quick", "Standard", "Comprehensive", or "Custom" — or ask any questions about the modes.	\N	\N	\N	\N	\N	\N	2026-05-31 18:40:55.500294+00
536009a8-45ec-4b50-85a6-5bbc640f846d	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	quick	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:40:59.090372+00
9ff302b7-40c9-4254-a522-0f278df69ae1	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Great — **Quick Mode** it is! We'll target 3–5 core requirements and keep things focused.\n✅ Phase 0 complete → Proceeding to Phase 1: Strategic Context\n**What problem are you trying to solve with this system?**	\N	\N	\N	\N	\N	\N	2026-05-31 18:41:03.563435+00
322c775e-15c7-4b55-b75d-9a8366f7736e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	our developers are spending significant time answering questions on our interop capabilities, as much as 20% of a sprint. We wan to add a chat agent/bot to our existing partner portal that indexes the documentation and then answers their questions. it should reword what it finds from the docs (since our wording clearly didn't help) whilst remaining factually correct and providing examples. It should appear in the bottom right on every page but also be available as a full page too.	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:41:17.987196+00
1c8b9723-9528-41e9-b8ed-cfe85fc85d4f	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	That's a really clear problem statement — a documentation Q&A bot to deflect repetitive interop questions from developers, freeing up your team's sprint capacity.\nWho are the primary users of this bot — is it **external partner developers** accessing your portal, your **internal team**, or both?	\N	\N	\N	\N	\N	\N	2026-05-31 18:41:23.833459+00
bfd1c522-2dcd-4095-875b-23e45c997d22	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	external partners, but some internal staff - we can focus just on external though as that will help everyone	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:41:35.192183+00
09cd95e5-2403-4e79-82c5-66b4a0e2a93f	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Got it — external partner developers are the primary users, with the understanding that solving for them benefits internal staff too.\nHow do you know this is a real problem — do you have any data or evidence to point to? For example, support ticket volumes, developer feedback, or sprint retrospective notes?	\N	\N	\N	\N	\N	\N	2026-05-31 18:41:39.692421+00
6e8d3d32-df4b-4b39-9507-1f6f2bce3d1b	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	we are spending 20% of sprint time (tracked) on this	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:42:18.298019+00
3c68f2de-91c2-4140-9660-035af7838877	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Solid evidence — 20% of sprint time tracked is a compelling, quantified baseline to work from.\nWhat happens if you **don't** solve this problem — what's the impact on the team or your partners over the next 6–12 months?	\N	\N	\N	\N	\N	\N	2026-05-31 18:42:22.611308+00
92ccfbfc-905f-4cbc-85fb-a38fd80c0bab	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	potential loss of revenue from partners giving up, developers burning out, not getting through the backlog	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:42:52.142447+00
61748379-2e6b-4d50-89c5-7beb4fae8a34	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	That's a strong business case — revenue risk, retention risk, and team sustainability all in play.\nWhat does **success look like** for this project? If you had to pick one number that would tell you "this worked", what would it be?	\N	\N	\N	\N	\N	\N	2026-05-31 18:42:57.457828+00
48ec2c03-3b05-43f2-a629-e7ee86af2107	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	less than 5% of sprint time spent answering questions	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:43:07.449398+00
54fd8c89-98a3-46bd-9cc3-fb4e8b5d367f	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Perfect — a clear, measurable North Star: **reduce sprint time spent answering interop questions from 20% → <5%**.\nWhat is your **timeline constraint** for getting this live?	\N	\N	\N	\N	\N	\N	2026-05-31 18:43:10.904534+00
8cd84687-630b-401a-9b03-dfa9b4e03571	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	2 sprints, timeboxed	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:43:15.379842+00
7fc76c80-349d-4340-982d-ac9a8c294597	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Two sprints, timeboxed — that's tight but well-suited to Quick Mode scope.\nWhat is your **biggest unknown or risk** right now?	\N	\N	\N	\N	\N	\N	2026-05-31 18:43:18.672145+00
26dedda3-3654-469f-ba40-bf927dd585ff	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	the implementation - is it LLM, embeddings, fine tuned model, RAG etc	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:44:40.261915+00
e5a2754c-23b7-41de-8a30-e1880f8351ef	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Understood — the AI/ML architecture decision is the top technical risk. I'll park that for Pipeline 03 Architecture to resolve.\nHere's a quick summary of what I've captured before we move on:\n---\n**STRATEGIC PRODUCT BRIEF**\n**Problem:** Developers spending ~20% of sprint time answering repetitive interop capability questions from external partners.\n**Who is affected:** External partner developers (and internal team by extension)\n**Evidence:** 20% sprint time tracked\n**Impact of inaction:** Partner revenue loss, developer burnout, backlog not progressing\n**Solution:** Chat bot/agent on the existing partner portal — indexes documentation, rewrites answers in clearer language with examples, available as a floating widget (bottom-right, every page) and as a full-page view.\n**Success defined as:** Sprint time answering interop questions drops to <5%\n**Measured by:** Sprint time tracking (same method as baseline)\n**Timeline:** 2 sprints, timeboxed\n**Top risk:** AI/ML implementation approach (LLM vs RAG vs embeddings — deferred to Pipeline 03)\n---\nDoes this accurately reflect what you've told me? If so, I'll move us into Phase 2.	\N	\N	\N	\N	\N	\N	2026-05-31 18:44:50.632383+00
64859a39-5b12-4de9-bd15-da1ab5dfdb06	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	yes	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:44:54.431422+00
e5259467-3f7f-4cf9-931b-f56b0410da05	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	✅ Phase 1 complete → Proceeding to Phase 2: Product Context & Project Setup\nI can see your project is **Q & A Interop Bot** (code: QABOT-001, compliance domain: Generic). I've saved a draft manifest to get us started.\nWhat **type of system** is this — would you classify it as a patient-facing app, clinical system, administrative system, integration/API, or something else? (Given the context, I'm expecting "partner-facing web application" but want to confirm your framing.)	\N	\N	\N	\N	\N	\N	2026-05-31 18:45:08.931037+00
ca845945-ea56-4451-b9b9-5bc288090e5c	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	what you said	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:45:19.550924+00
f28ced6e-6538-4aec-852d-0e957e335321	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	yes	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:32:31.207264+00
113ba23b-93c3-453a-a786-3da71b3c59e7	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Confirmed — **partner-facing web application** with an embedded chat interface.\nIs this a **new system**, or is it being added to an existing partner portal?	\N	\N	\N	\N	\N	\N	2026-05-31 18:45:23.185545+00
a4643606-8d66-48c5-ac4a-e1ef2af87aa8	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	added to the existing partner portal based on astro/starlight. It does have a react wrapper around it already though so that we could integrate with EMIS-X users for partner onboarding (so they can request client ids/secrets)	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:46:06.257894+00
9d227393-9f42-4e69-bec0-f5cfd74b3d20	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Great context — so this is an **additive feature** to an existing Astro/Starlight portal, with a React wrapper already in place for EMIS-X integration.\nWhat is the **regulatory classification** of this system? Given it's a developer portal Q&A bot (not directly handling patient data), I'd expect "Not safety-critical" — but please confirm.	\N	\N	\N	\N	\N	\N	2026-05-31 18:46:12.035917+00
0c98ed48-1c39-488f-8aed-34cfe27e135e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	yes	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:46:16.974928+00
5ef2c8b7-3276-4a48-9c6c-d7d08c908009	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Confirmed — **not safety-critical**. No DCB0129/0160 obligations for this system.\n✅ Phase 2 complete → Proceeding to Phase 3: Personas & Users\nLet me get the existing manifest and update it before we move on.\nFor the primary persona — the **external partner developer** — what are their main goals when visiting the portal? What are they typically trying to do?	\N	\N	\N	\N	\N	\N	2026-05-31 18:46:23.646903+00
75c678ac-9478-4cf0-8f62-df6b73597d7f	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	integrate with our solutions - apis and we have a system almost identical to SMART on FHIR App Launch	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:47:07.601923+00
c182662b-838b-4952-b797-e358ab41fcc0	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Got it — partner developers are trying to integrate with your APIs and your SMART-on-FHIR-style app launch system. They need accurate, fast answers to unblock their integration work.\nWhat are their **biggest pain points** with the current portal documentation? (Beyond the obvious "they can't find answers easily" — any specific patterns you've noticed in the questions they ask?)	\N	\N	\N	\N	\N	\N	2026-05-31 18:47:12.682402+00
58da0195-4e10-4e41-8f25-f0f3985f696d	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	they don't always seem to understand the documentation, we'd like the LLM to reword/explain differently whilst still bein factual	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:47:48.886756+00
f0ac3b3c-7643-4c48-b767-a37c78682eb0	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	That's a really important nuance — it's not just retrieval, it's **reformulation**: the bot needs to explain concepts differently from how the docs are written, while staying factually grounded.\nHow **tech-savvy** are these partner developers — are they experienced API/FHIR integrators, or do they range from junior to senior?	\N	\N	\N	\N	\N	\N	2026-05-31 18:47:53.128573+00
8200a4ee-00eb-4610-8999-fa818ca35ff4	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	range, but still tech folk	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:48:02.116823+00
0596a62c-059f-4508-baad-5862167ff3b8	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Got it — technically literate but varying levels of experience with FHIR/OAuth concepts specifically.\n✅ Phase 3 complete → Proceeding to Phase 4: Core Workflow\nWalk me through the **core user workflow** step by step — from a partner developer landing on the portal through to getting their question answered by the bot.	\N	\N	\N	\N	\N	\N	2026-05-31 18:48:06.851862+00
3cc115d3-878b-4d59-bff5-8e8473e8b4ac	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	come to site, read docs, widget available in bottom right on all pages (or full screen on a dedicated page) where they can ask questions relevant to the material. If they can't get an answer, they can escalate by providing an email address. That will send us a notification into our Teams channel via a webhook	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:49:26.112184+00
0216c4f6-bb67-44b8-bde1-d33973fe4015	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Excellent — clear end-to-end flow. The Teams webhook escalation path is a key integration point I'll note.\nIf a developer starts a conversation with the bot, then navigates to a different page mid-conversation — should the **conversation history persist** as they move around the site, or does each page start a fresh session?	\N	\N	\N	\N	\N	\N	2026-05-31 18:49:32.053078+00
91393e91-6dab-44b8-8832-45b0775985b7	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	persist until they close their browser	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:49:43.336967+00
816c0388-90a5-44f0-8441-422377ce4c8a	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Perfect — session-scoped persistence tied to browser session lifecycle.\nAt what point should the conversation be **fully reset**? For example: closing the browser, explicitly clicking a "clear conversation" button, or both?\nPerfect — session-scoped persistence tied to browser session lifecycle.\nAt what point should the conversation be **fully reset**? For example: closing the browser, explicitly clicking a "clear conversation" button, or both?	\N	\N	\N	\N	\N	\N	2026-05-31 18:49:48.473179+00
a5b7a875-4cb6-401d-8347-d6c331bcfe30	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	both	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:49:55.784311+00
8d4ce3ec-71d7-47c1-b828-5c2be706403c	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Got it — reset on browser close AND via an explicit "clear conversation" button in the widget.\n✅ Phase 4 complete → Proceeding to Phase 5: Requirements Elicitation\nNow let's capture the core requirements. Given Quick Mode, we're targeting 3–5. Let me start with the most obvious one.\n**Requirement 1:** The chat widget — describe in one sentence what it needs to do.	\N	\N	\N	\N	\N	\N	2026-05-31 18:50:02.406048+00
2883ced5-b8e0-4a31-8d05-d1b2da2ffb49	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	should be a conversational bot, that can take the context, refernce the docs, help answer their questions by giving them the answer in different ways with examples and citation	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:50:34.007097+00
21ae5bbb-c0bf-4fbf-921f-7dab8c15a50c	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Great. Let me probe for evaluation criteria before we confirm this one.\nWhat's a specific example of a **valid question** a partner developer might ask — and what would a good response look like? (Give me as concrete an example as you can — exact question, expected answer shape.)	\N	\N	\N	\N	\N	\N	2026-05-31 18:50:36.75427+00
c122b6d5-1458-43c7-8af9-eceed345d217	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	"I've got a backend API that I want to make calls from to EMIS APIs, which authentication flow should I be using?"	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:51:13.143647+00
0736a6c1-8776-4868-a482-d79a6fd05169	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Good example. And what would a **bad or invalid input** look like — something the bot should reject or gracefully decline to answer? For example, a question completely unrelated to your interop docs?	\N	\N	\N	\N	\N	\N	2026-05-31 18:51:15.403715+00
1a9482d0-8a70-471f-8d7f-96f4f64823a3	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	"whats the weather like today?"	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:51:26.244606+00
d6e0093f-d268-4b3e-98ad-f969644ad574	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Perfect. And what should the bot say in that case — something like "I can only answer questions about EMIS interoperability" or do you have a preferred message?	\N	\N	\N	\N	\N	\N	2026-05-31 18:51:27.941204+00
3d6bf088-2dd9-478e-af1c-99c967f35cc9	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	something polite, you decide	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:51:38.625358+00
a4cd682b-8a9e-44c7-9c8b-0cb6efad4a5d	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Noted — I'll specify a polite out-of-scope deflection message in the eval spec.\nWhat **must** be in a successful response? You mentioned citations — should every answer include a link back to the specific doc page it drew from?	\N	\N	\N	\N	\N	\N	2026-05-31 18:51:41.217576+00
e2c8295e-0c03-44ae-8856-39fb5416c2ef	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	yes, always cite the docs	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:51:50.62541+00
c61b3497-77ab-45ca-b4d0-de7f612523ae	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Always cite the source doc — mandatory, not optional.\nShould citations include just a **link to the page**, or also a specific section/heading within that page?	\N	\N	\N	\N	\N	\N	2026-05-31 18:51:52.817205+00
4f07a1bd-812d-40f2-8735-93818ea0dac7	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	link to the page and the key lines from it	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:52:06.622649+00
f42031a0-ce65-449f-b176-e644e719f92c	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Got it — citation must include the page URL and the relevant quoted lines from the source.\nWhat's your **response time expectation** — at what point would a partner developer consider the bot "broken" or too slow?	\N	\N	\N	\N	\N	\N	2026-05-31 18:52:08.933001+00
47177fc9-ee80-4b1e-9e4c-77a107f226ca	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	under 10s	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 18:52:16.481579+00
43df59cc-332e-4dfa-bd09-533bf91917f4	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	10 seconds is the threshold — I'll set p95 < 10s as the SLO.\nLet me now confirm Requirement 1 before we save it:\n---\n**Requirement 1: Conversational Q&A Chat Widget**\n**Acceptance Criteria:**\n- ✓ Widget appears in bottom-right corner on every portal page\n- ✓ Bot answers questions by reformulating doc content in clearer language with examples\n- ✓ Every answer includes a citation: page URL + relevant quoted lines from source\n- ✓ Out-of-scope questions (e.g. "what's the weather?") receive a polite deflection: *"I can only help with questions about EMIS interoperability — please try asking about our APIs or authentication flows."*\n- ✓ Conversation persists across page navigation until browser close or explicit "Clear conversation" button press\n**Evaluation Criteria (Deterministic):**\n- ✓ REJECT: "What's the weather like today?" → polite out-of-scope deflection, no doc citation\n- ✓ ACCEPT: "Which auth flow should I use for a backend API?" → answer with reformulated explanation + example + citation (URL + quoted lines)\n- ✓ MUST RETURN: answer text, citation URL, quoted source lines\n- ✓ TIMING: p95 response < 10s\n**Clinical Safety:** N/A (not safety-critical)\n**Information Governance:** No patient data; email address captured on escalation (GDPR Article 6 lawful basis TBD)\n**Security:** No auth required to use widget; HTTPS/TLS in transit\n**Observability:** Response success rate, p95 latency, out-of-scope deflection rate\n**Frontend:** Floating widget (bottom-right), full-page mode, @emisgroup/ui-* components, i18n, jest-axe\n**Priority:** Must Have | **Effort:** High | **Risk:** High\nIs this correct?	\N	\N	\N	\N	\N	\N	2026-05-31 18:52:27.882064+00
fa0bdd38-f195-4de8-9972-bdedecf360ff	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	yes	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:24:45.247883+00
12ec3f0b-852f-498f-982d-c68ce9f1fb67	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	✅ REQ-001 confirmed — saving now.\n✅ REQ-001 saved.\nOn to Requirement 2. The escalation flow — when a partner developer can't get an answer from the bot and provides their email to escalate. Describe in one sentence what needs to happen.	\N	\N	\N	\N	\N	\N	2026-05-31 19:27:26.521335+00
8d43bb1f-3a58-4c91-9353-20fafd4a5e3e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	capture an email, send it to our teams channel with the chat history so we can respond	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:27:53.778342+00
bfa1773d-65c1-4a58-9acc-049bfd14e875	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Good. What's a specific example of an **invalid email** that should be rejected — and what error message should the user see?	\N	\N	\N	\N	\N	\N	2026-05-31 19:27:58.552894+00
7dbf8328-18d6-4e50-b581-f68fd7e5288c	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	why you asking that? There are standard patterns for email addresses ffs	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:28:26.965928+00
277c9279-71f4-494a-8f28-bf627a488563	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Fair point — standard email format validation, no need to labour it. I'll apply RFC 5322 pattern validation with a standard "Please enter a valid email address" error.\nShould the bot ask for the email **proactively** (e.g. after N failed answers) or only when the user explicitly clicks an "escalate" or "I need more help" button?	\N	\N	\N	\N	\N	\N	2026-05-31 19:28:30.093111+00
12d1fe6b-1d5f-41a1-9058-bc404be3d636	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	if it can't answer from the material	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:28:51.736234+00
24f30046-e17f-40b7-8794-2b46d96f44cb	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Got it — bot-triggered escalation when it determines it cannot answer from the available documentation.\nWhat should the **Teams notification** contain — just the email and chat history, or anything else? For example, the page they were on, timestamp, the specific question that triggered the escalation?	\N	\N	\N	\N	\N	\N	2026-05-31 19:28:54.296507+00
39b55224-f164-420a-bb9c-61987678b9a0	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	all of that metadata	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:29:02.192342+00
c3435e9d-669c-46ea-8127-39383dbcdb62	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Perfect — full context package: email, chat history, triggering question, current page URL, and timestamp.\nWhat should the **user see** after they submit their email — a confirmation message, and should the bot indicate how quickly they can expect a response?	\N	\N	\N	\N	\N	\N	2026-05-31 19:29:04.43204+00
2a8aed33-9aba-4dd3-8a03-e0582ca4709f	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	just a confirmation - and we should send an email to the provided address as confirmation	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:29:32.572207+00
53296695-0ed1-499b-a4f7-ee4e3c4d8004	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Good — so two notifications fire on escalation: Teams webhook (internal) and a confirmation email to the partner developer.\nWhat's a **valid escalation scenario** the eval spec should test — and what should the confirmation message say to the user in the widget?	\N	\N	\N	\N	\N	\N	2026-05-31 19:29:35.183393+00
60ee0a90-ffde-4445-b80a-7d6475dbe361	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	eval spec?	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:29:52.968964+00
1e879cd6-1f3a-42af-9fa1-54514d768005	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Sorry — I mean: what should the confirmation message say to the user in the chat widget after they submit their email? For example: *"Thanks, we've received your question and will get back to you shortly."* Or do you have preferred wording?	\N	\N	\N	\N	\N	\N	2026-05-31 19:29:55.142372+00
759ff244-2967-4773-a85d-eedcbb61cb34	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	just something polite	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:30:04.899809+00
c82fad12-60e3-4f6c-8048-b42fe4f6626e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	✅ REQ-003 confirmed — saving now.\n✅ REQ-003 saved.\nWe have 3 solid requirements. In Quick Mode we're targeting 3–5. Are there any other requirements you want to capture — for example, **documentation indexing** (how the bot gets its knowledge) or **conversation management** (history, clear button)? Or are you happy to move to non-functional requirements with what we have?	\N	\N	\N	\N	\N	\N	2026-05-31 19:33:13.662169+00
4415f0a9-cbd4-49e0-b3f2-18ae5da0b6b1	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	keep going	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:33:36.152046+00
da919e1c-4b4d-4e22-8831-a1dfa7c9e737	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Good — let's capture the documentation indexing requirement next.\n**Requirement 4: Documentation Indexing**\nIn one sentence, how should the bot get its knowledge from your docs — is this a one-time index, a scheduled refresh, or triggered manually when docs are updated?	\N	\N	\N	\N	\N	\N	2026-05-31 19:33:38.551464+00
f44b8293-8313-4554-8d46-a4785f760b94	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	when we update the dev portal, it should be re-indexed. CI is github actions	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:34:08.000333+00
ddb690c2-dcfd-4c97-a417-5554176b97c6	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Noted — I'll draft a polite confirmation message in the spec.\nLet me confirm Requirement 2:\n---\n**Requirement 2: Escalation to Human Support**\n**Acceptance Criteria:**\n- ✓ When the bot cannot answer from the documentation, it prompts the user to escalate by providing their email address\n- ✓ Email address validated against standard RFC 5322 pattern; invalid format shows "Please enter a valid email address"\n- ✓ On submission, a Teams channel notification is sent via webhook containing: email, full chat history, triggering question, current page URL, and timestamp\n- ✓ A confirmation email is sent to the partner developer's provided address\n- ✓ User sees a polite confirmation in the widget: *"Thanks for reaching out — we've received your question and will be in touch soon."*\n**Evaluation Criteria (Deterministic):**\n- ✓ REJECT: Invalid email format → "Please enter a valid email address", no notification sent\n- ✓ ACCEPT: Valid email submitted after bot fails to answer → Teams webhook fires + confirmation email sent + confirmation message shown\n- ✓ MUST SEND (Teams): email, chat history, triggering question, page URL, timestamp\n- ✓ TIMING: Webhook + confirmation email dispatched within 5s of submission\n**Clinical Safety:** N/A\n**Information Governance:** Email address is personal data — GDPR Article 6 lawful basis TBD (legitimate interest likely); data minimisation applies\n**Security:** Email stored only for escalation purposes; HTTPS in transit; webhook URL must be secured\n**Observability:** Escalation rate, webhook delivery success rate, confirmation email delivery rate\n**Frontend:** Email input field, submit button, confirmation state — @emisgroup/ui-* components, i18n, jest-axe\n**Priority:** Must Have | **Effort:** Medium | **Risk:** Medium\nIs this correct?	\N	\N	\N	\N	\N	\N	2026-05-31 19:30:12.326181+00
c7acae03-df1f-43fb-b5a9-31b56dfed959	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	yes	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:30:15.671171+00
77de9fa6-61c4-468c-9f5f-081c29c48f37	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	✅ REQ-002 confirmed — saving now.\n✅ REQ-002 saved.\nOn to Requirement 3. The **full-page chat mode** — the bot is available both as a floating widget and as a dedicated full-page view. In one sentence, what does the full-page experience need to do differently from the widget?	\N	\N	\N	\N	\N	\N	2026-05-31 19:31:13.397885+00
b93c3595-3f3b-423b-9f67-ffc6b811bf51	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	be full screen?	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:31:28.310809+00
463ff6eb-2d72-4c15-8cc8-d92ecdfe6c84	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Ha — fair enough! Same functionality, just full-screen real estate.\nShould the **conversation history persist** between the widget and full-page views — i.e. if I start chatting in the widget then open the full-page view, do I see the same conversation?	\N	\N	\N	\N	\N	\N	2026-05-31 19:31:31.016437+00
0141f93d-5fe4-47fc-a69b-3e5bb240dbc3	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	yes	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:31:38.054045+00
b59b23a5-8926-4c7e-a547-26b675fd3ece	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Perfect — unified session, same conversation regardless of which view you're in.\nShould there be a way to **navigate between** the widget and full-page view — for example, an "expand" button in the widget that opens the full-page, and a "back" or "minimise" button on the full-page?	\N	\N	\N	\N	\N	\N	2026-05-31 19:31:41.015254+00
f01d3630-5034-4641-a062-d34c193a02fb	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	can do	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:31:50.082635+00
8505e2d0-c6fa-4579-bf2d-da6f1292a335	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Got it — expand/collapse navigation between the two views.\nLet me confirm Requirement 3:\n---\n**Requirement 3: Full-Page Chat Mode**\n**Acceptance Criteria:**\n- ✓ A dedicated full-page chat view is available at a fixed portal route (e.g. `/chat`)\n- ✓ Full-page view provides the same Q&A functionality as the widget — same bot, same citations, same escalation flow\n- ✓ Conversation history is shared between widget and full-page view within the same browser session\n- ✓ Widget includes an "expand" button to open the full-page view\n- ✓ Full-page view includes a "back" / "minimise" control to return to the widget\n**Evaluation Criteria (Deterministic):**\n- ✓ ACCEPT: Conversation started in widget → expand to full-page → same messages visible\n- ✓ ACCEPT: Conversation started in full-page → minimise to widget → same messages visible\n- ✓ ACCEPT: Full-page route accessible directly via URL\n- ✓ TIMING: Transition between widget and full-page < 300ms (no reload)\n**Clinical Safety:** N/A\n**Information Governance:** No additional data captured beyond REQ-001/REQ-002\n**Security:** No auth required; HTTPS in transit\n**Observability:** Full-page view usage rate vs widget usage rate, session continuity success rate\n**Frontend:** Full-page route, expand/collapse controls — @emisgroup/ui-* components, i18n, jest-axe\n**Priority:** Must Have | **Effort:** Low | **Risk:** Low\n**Depends On:** REQ-001\nIs this correct?	\N	\N	\N	\N	\N	\N	2026-05-31 19:31:56.321902+00
fe9ed54d-c545-4697-b27f-d74a3bef6bf5	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	auth is required	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:32:03.752296+00
dde22a87-26d5-44d4-bbbc-a976bf4f9e3d	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Good catch — what authentication is required? Is this the existing EMIS-X/CIS2 auth already in place on the portal, or is the bot accessible to unauthenticated visitors?	\N	\N	\N	\N	\N	\N	2026-05-31 19:32:05.958094+00
bc2cd2d6-5270-4210-a334-16da751a9d61	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	EMIS-X users	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:32:12.787392+00
42a3eb2c-268c-446d-a854-9adf8d4ae621	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Got it — the bot is only accessible to authenticated EMIS-X users. Unauthenticated visitors should not be able to use the chat.\nDoes this apply to **both** the widget and full-page view — i.e. REQ-001 also needs updating to require EMIS-X authentication?	\N	\N	\N	\N	\N	\N	2026-05-31 19:32:15.072873+00
f52055b0-52c6-46ce-bac9-e1be99ac63ba	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	both	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:32:18.243397+00
9b7ee02d-5efe-4389-9ac8-d72ef28a4af8	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Noted — I'll apply EMIS-X authentication to both REQ-001 and REQ-002 as well. Let me update Requirement 3:\n---\n**Requirement 3: Full-Page Chat Mode**\n**Acceptance Criteria:**\n- ✓ A dedicated full-page chat view is available at a fixed portal route (e.g. `/chat`)\n- ✓ Full-page view provides the same Q&A functionality as the widget — same bot, same citations, same escalation flow\n- ✓ Conversation history is shared between widget and full-page view within the same browser session\n- ✓ Widget includes an "expand" button to open the full-page view\n- ✓ Full-page view includes a "back" / "minimise" control to return to the widget\n- ✓ Both widget and full-page view require EMIS-X authentication — unauthenticated users cannot access the bot\n**Evaluation Criteria (Deterministic):**\n- ✓ REJECT: Unauthenticated request to access widget or full-page → redirect to EMIS-X login\n- ✓ ACCEPT: Authenticated EMIS-X user → bot accessible in both widget and full-page\n- ✓ ACCEPT: Conversation started in widget → expand to full-page → same messages visible\n- ✓ ACCEPT: Full-page route accessible directly via URL (authenticated users only)\n- ✓ TIMING: Transition between widget and full-page < 300ms (no reload)\n**Clinical Safety:** N/A\n**Information Governance:** No additional data captured beyond REQ-001/REQ-002\n**Security:** EMIS-X authentication required for all access; HTTPS in transit; unauthenticated access blocked\n**Observability:** Full-page view usage rate vs widget usage rate, session continuity success rate, auth failure rate\n**Frontend:** Full-page route, expand/collapse controls — @emisgroup/ui-* components, i18n, jest-axe\n**Priority:** Must Have | **Effort:** Low | **Risk:** Low\n**Depends On:** REQ-001\nIs this correct?	\N	\N	\N	\N	\N	\N	2026-05-31 19:32:23.818656+00
0d7c15cf-08a8-45b9-9506-61683dba7c50	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	So indexing is triggered automatically as part of the GitHub Actions CI pipeline when the portal is updated — not a scheduled job or manual process.\nShould the bot be **unavailable or degraded** during a re-index, or should it continue serving answers from the previous index until the new one is ready?	\N	\N	\N	\N	\N	\N	2026-05-31 19:34:10.638622+00
a2e29a63-2faf-48c4-a746-b0eff2168133	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	the indexing should happen before the new version of the site goes out	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:34:31.550293+00
c3c9257f-cffe-4ca3-854b-ff8afb08eb55	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Perfect — indexing is a **pre-deployment gate** in the CI pipeline. The new index must be built and ready before the site deployment proceeds.\nWhat should happen if the **indexing step fails** in CI — should the deployment be blocked, or should it deploy with the previous index and alert the team?	\N	\N	\N	\N	\N	\N	2026-05-31 19:34:34.230113+00
86b7c96c-ea79-43c9-b544-f71a915d5d0e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	block	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:34:41.744543+00
f706f500-1a91-4be1-b74b-4b0b46e65f76	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Clean and safe — failed indexing blocks deployment entirely.\nHow large is the documentation corpus roughly — are we talking tens of pages, hundreds, or thousands?	\N	\N	\N	\N	\N	\N	2026-05-31 19:34:43.732115+00
48175281-19e7-4b9d-9cb6-2babd666bc22	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	10s	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:34:49.674581+00
58597c87-2fc6-4002-a295-6e828c0f3221	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Tens of pages — small corpus, fast indexing expected.\nLet me confirm Requirement 4:\n---\n**Requirement 4: Documentation Indexing via CI**\n**Acceptance Criteria:**\n- ✓ Documentation is indexed automatically as part of the GitHub Actions CI pipeline on every portal update\n- ✓ Indexing runs as a pre-deployment gate — new index must complete successfully before site deployment proceeds\n- ✓ If indexing fails, the CI pipeline is blocked and deployment does not proceed\n- ✓ Bot continues serving from the previous index until the new index is successfully built and deployed\n- ✓ Corpus size: tens of pages — indexing expected to complete within a reasonable CI step duration\n**Evaluation Criteria (Deterministic):**\n- ✓ REJECT: Indexing step fails → CI pipeline blocked, deployment does not proceed, team notified\n- ✓ ACCEPT: Indexing step succeeds → deployment proceeds with fresh index\n- ✓ ACCEPT: Bot answers questions using most recently deployed index\n- ✓ TIMING: Indexing step completes within 5 minutes (CI timeout threshold)\n**Clinical Safety:** N/A\n**Information Governance:** Documentation content is non-personal; no GDPR obligations on indexing\n**Security:** GitHub Actions pipeline access controlled via repository permissions; index artefact stored securely\n**Observability:** Indexing success/failure rate, indexing duration, CI pipeline step timing\n**Frontend:** N/A — backend/CI process\n**Priority:** Must Have | **Effort:** Medium | **Risk:** High (blocks deployment if broken)\n**Depends On:** REQ-001\nIs this correct?	\N	\N	\N	\N	\N	\N	2026-05-31 19:34:57.044634+00
d1d717c5-ca6b-4f93-a493-df8cce5b34d8	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	yes	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:34:58.942443+00
bfea2834-3989-468e-91ef-d33a1563972a	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	✅ REQ-004 confirmed — saving now.\n✅ REQ-004 saved.\nWe have 4 solid requirements. One more would round this out nicely. I'm thinking **conversation management** — the clear conversation button, session state, and the "clear on browser close" behaviour you described. Shall we capture that as REQ-005, or is there another requirement you'd prioritise over it?	\N	\N	\N	\N	\N	\N	2026-05-31 19:35:35.023994+00
7f1a5e13-a7a8-4f3f-b494-78ca5339e0dc	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	ok	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:35:50.226927+00
f5895d71-0452-4482-b40c-7bd851e26caf	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	**Requirement 5: Conversation Management**\nIn one sentence — beyond clearing on browser close and the explicit clear button, is there anything else conversation management needs to handle? For example, a maximum conversation length, or a timeout after inactivity?	\N	\N	\N	\N	\N	\N	2026-05-31 19:35:52.326203+00
455ba970-b4a5-4f27-810c-2900dfc93ff6	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	no	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:36:25.881125+00
b4a64b1b-bbe0-4b0b-a830-2d3fcf64a50a	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Simple and clean. Let me confirm Requirement 5:\n---\n**Requirement 5: Conversation Management**\n**Acceptance Criteria:**\n- ✓ Conversation history persists across page navigation within the same browser session\n- ✓ Conversation history persists between widget and full-page views within the same session\n- ✓ An explicit "Clear conversation" button is available in both the widget and full-page view\n- ✓ Clicking "Clear conversation" immediately resets the conversation to a clean state\n- ✓ Closing the browser clears the conversation — no history persists on next visit\n**Evaluation Criteria (Deterministic):**\n- ✓ ACCEPT: User navigates between pages → conversation history intact\n- ✓ ACCEPT: User clicks "Clear conversation" → conversation resets to empty state, confirmation prompt shown before clearing\n- ✓ ACCEPT: User closes browser, reopens portal → conversation starts fresh with no previous history\n- ✓ REJECT: Conversation state persists after browser close → fail (sessionStorage or equivalent, not localStorage)\n- ✓ TIMING: Clear action completes < 300ms\n**Clinical Safety:** N/A\n**Information Governance:** Conversation data held in sessionStorage only — not persisted server-side; no GDPR retention obligation\n**Security:** No conversation data written to server; sessionStorage scoped to tab/window\n**Observability:** Clear conversation event rate, session duration, conversation length (message count)\n**Frontend:** Clear button, confirmation prompt — @emisgroup/ui-* components, i18n, jest-axe\n**Priority:** Must Have | **Effort:** Low | **Risk:** Low\n**Depends On:** REQ-001, REQ-003\nIs this correct?	\N	\N	\N	\N	\N	\N	2026-05-31 19:36:33.124908+00
3f3525be-a739-411b-a303-262c353d7aa7	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	yes	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:36:35.769029+00
8f4e2728-5153-4695-beb8-60d26ad44897	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	✅ REQ-005 confirmed — saving now.\n✅ REQ-005 saved.\nThat gives us 5 solid requirements. Let me now run the pre-Phase 6 completeness sweep before we move to non-functional requirements. A few quick checks:\n1. **Billing/subscription** — not applicable here, correct?\n2. **Data portability** — partners don't need to export their conversation history, correct?\n3. **Rate limiting** — should there be any limit on how many questions a partner can ask per session or per day to prevent abuse?	\N	\N	\N	\N	\N	\N	2026-05-31 19:37:22.230974+00
cf3621d3-5efd-441f-ac16-0fa0626f06ef	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	yes to 3, put some sensible safety in	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:37:47.092002+00
e580b3f3-161e-4988-957b-d61c8fdb846a	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Good call. I'll add rate limiting as a non-functional requirement rather than a standalone REQ — sensible defaults: e.g. 50 messages per session, 200 per day per authenticated user, with a polite "you've reached your limit" message.\n✅ Pre-phase sweep complete → Proceeding to Phase 6: Non-Functional Requirements\nWhat **availability/uptime** is required for the bot — is 99.9% sufficient, or does it need to match your portal's existing SLA?	\N	\N	\N	\N	\N	\N	2026-05-31 19:37:53.582075+00
37ec36de-ce3b-42f2-87f1-9c1e1aa43909	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	99.9 is more than sufficient, if there's an outage we just show the escalate to team	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:38:13.185399+00
a299acca-cbdf-4879-8670-349896d924b3	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Smart degraded mode — bot outage surfaces the escalation form rather than an error. I'll capture that as a graceful degradation behaviour.\nWhat **browsers and devices** must be supported?	\N	\N	\N	\N	\N	\N	2026-05-31 19:38:15.491826+00
5aceac4e-c0bb-463a-9fd6-7da95141c9b1	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	same as EMIS-X	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:38:21.368945+00
de345311-6c68-40dd-a977-d8c91a1907f4	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Noted — browser/device support inherits the EMIS-X platform matrix. I'll reference that rather than define it independently.\nWhat **accessibility level** is required?	\N	\N	\N	\N	\N	\N	2026-05-31 19:38:23.490345+00
4269bcf6-fe0c-4157-8282-4920c334edd2	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	same again	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:38:31.431+00
be5a50b3-6aef-40f2-af2f-75fb383db5f7	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Consistent — WCAG 2.1 Level AA, matching EMIS-X platform standard.\nAre there any **data retention** requirements for escalation records — how long should the email addresses and chat histories captured during escalation be kept?	\N	\N	\N	\N	\N	\N	2026-05-31 19:38:34.767845+00
8e59ba54-4938-41a0-be32-ed79a80c8421	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	whats reasonable? 1 year?	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:38:52.809607+00
78d52d91-5160-440e-abab-96714b6e5025	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	1 year is reasonable for support escalation records — long enough to track patterns, short enough to minimise GDPR exposure. I'll set that as the retention period.\nAre there any **performance requirements beyond the 10s response time** we already captured — for example, page load time for the full-page chat view, or widget initialisation time?	\N	\N	\N	\N	\N	\N	2026-05-31 19:38:55.846061+00
46859214-573f-4774-a5c9-77e4892e0f82	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	no	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:39:00.311899+00
b210cd94-7f1e-488c-b588-808795fb87fb	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Clean — 10s p95 response time is the only latency SLO we need.\n✅ Phase 6 complete → Proceeding to Phase 7: Integration Points\nWe've already identified two integration points. Let me confirm them and check for any others:\n1. **Teams webhook** — escalation notifications\n2. **GitHub Actions** — CI/CD indexing pipeline\nAre there any **other external systems** this bot needs to integrate with — for example, an email service provider for the confirmation emails, or an existing identity provider beyond EMIS-X?	\N	\N	\N	\N	\N	\N	2026-05-31 19:39:04.262785+00
b596bcad-080c-4f75-8c4d-8bc1ad9334a0	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	just AWS SES to send the emails	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:39:21.313092+00
3131dd85-3447-4dea-ae85-e782af149630	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Perfect — AWS SES for confirmation emails. That's our third integration point.\nWhat happens if **AWS SES is unavailable** when a user escalates — should the escalation still proceed (Teams webhook fires, email silently fails) or should the user be informed the confirmation email couldn't be sent?	\N	\N	\N	\N	\N	\N	2026-05-31 19:39:23.821868+00
afc3ba33-2255-4a6f-9c0d-6f86fc9a83f5	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	just send to team, don't let them know anyhting went wrong	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:39:38.886651+00
ad92f255-af0b-4529-bde0-61c360eb34a1	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Got it — SES failure is silent to the user; Teams webhook still fires and that's the source of truth for escalation.\nWhat happens if the **Teams webhook is unavailable** when a user escalates — should the escalation be queued and retried, or does it fail silently too?	\N	\N	\N	\N	\N	\N	2026-05-31 19:39:41.1836+00
5dee4e56-9cb8-443d-b1b6-ed9030a55f13	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	tell them the system is down and to try later	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:40:03.733606+00
926cbcd9-97e6-409a-b666-920a6b71ac28	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Good — Teams webhook failure is user-visible with a polite "system is currently unavailable, please try again later" message. That's the critical path for escalation.\n✅ Phase 7 complete → Proceeding to Phase 8: Assumptions & Risks\nWhat are your **biggest assumptions** about this project that could turn out to be wrong?	\N	\N	\N	\N	\N	\N	2026-05-31 19:40:06.200763+00
3da19940-c6d7-4da7-aee8-dfb95beeb77d	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	no assumptions	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:40:17.378976+00
e4eb90b3-1f0e-4207-b0af-54d007d0702f	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Noted. What are the **highest risks** you see — technical or otherwise — that could cause this project to fail within the 2-sprint timebox?	\N	\N	\N	\N	\N	\N	2026-05-31 19:40:19.597832+00
eca9b0c8-b9c1-4efa-a068-a06b14072cdd	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	tech - llm/rag/embeddings etc	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:40:38.272478+00
8b2571b1-8007-4d42-b210-c67b688c0511	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Consistent with what you flagged earlier — the AI implementation approach is the top risk. Pipeline 03 Architecture needs to resolve this before build starts.\n✅ Phase 8 complete → Proceeding to Phase 9: Constraints\nBeyond the **2-sprint timebox**, are there any other constraints we haven't captured — budget, specific infrastructure requirements, or technology restrictions?	\N	\N	\N	\N	\N	\N	2026-05-31 19:40:41.400673+00
9ed58bca-032b-4c9c-843c-cd5e9fe644ac	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	cheap as possible	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:41:32.971309+00
15bb92fa-529e-4530-9170-b96717ee4fd2	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Cost-optimised — I'll note that the AI implementation choice in Pipeline 03 must factor in running costs, favouring lightweight/efficient approaches over expensive hosted LLM APIs where possible.\nAny **technical constraints** — for example, must it run on AWS (given SES is already in use), or is the infrastructure platform open?	\N	\N	\N	\N	\N	\N	2026-05-31 19:41:36.323849+00
b983eba7-80f8-4d57-8dbd-47c45860dcb9	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	all aws and eu-west-2	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:41:48.02662+00
8983c49d-f9af-42fd-b126-a8d58c5cd444	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Confirmed — AWS, eu-west-2 region. That's important for GDPR data residency too.\n✅ Phase 9 complete → Proceeding to Phase 10: Success Metrics\nWe already have the North Star metric — sprint time answering interop questions dropping from 20% to <5%. Are there **2-3 additional metrics** you'd want to track to know the bot is working well?	\N	\N	\N	\N	\N	\N	2026-05-31 19:41:51.30422+00
3dfaf414-32dc-4047-84e3-c087bec5f23b	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	no	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:41:54.85092+00
05cfc02a-4278-4569-81e9-a4cfd91d02c8	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Perfectly focused — North Star metric is sufficient.\n✅ Phase 10 complete.\nLet me now run the **cross-cutting requirements gate** before finalising:\n| Cross-cutting concern | Status |\n|---|---|\n| Global error handling UX (bot outage → escalation form, Teams webhook failure → user message) | ✅ Captured in REQ-002 + NFRs |\n| Session lifecycle (browser close clears session) | ✅ REQ-005 |\n| Shell-level loading states | ⚠️ Not explicitly captured |\n| Offline / degraded network behaviour (bot outage → escalation) | ✅ Captured in Phase 6 |\n| Multi-step wizard abandon states | ✅ N/A — no multi-step wizards |\nOne gap: **loading states** — when the bot is processing a question (potentially up to 10s), what should the user see? A typing indicator, a spinner, or something else?	\N	\N	\N	\N	\N	\N	2026-05-31 19:42:00.456246+00
ea339cb1-fd0d-4f72-a8dc-0f9046f8784b	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	loading spinner, and if it is using tool calls tell the user what its doing	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:42:26.588588+00
9bfdbbf1-3803-41a2-a6f3-5e48559a31f7	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Great — so two loading states: a spinner for general processing, and contextual status messages when the bot is executing tool calls (e.g. "Searching documentation…", "Retrieving examples…").\n✅ Cross-cutting requirements gate cleared. 1 gap found and addressed — adding as REQ-006.\nLet me confirm Requirement 6:\n---\n**Requirement 6: Bot Loading & Status States**\n**Acceptance Criteria:**\n- ✓ While the bot is processing a response, a loading spinner is displayed in the chat interface\n- ✓ When the bot is executing tool calls (e.g. searching documentation, retrieving examples), a contextual status message is shown to the user (e.g. "Searching documentation…", "Retrieving examples…")\n- ✓ Status messages update in real-time as the bot progresses through tool calls\n- ✓ Spinner and status messages disappear when the response is ready\n- ✓ If the bot times out (>10s), a polite error message is shown: *"Sorry, that's taking longer than expected. Please try again or escalate to our team."*\n**Evaluation Criteria (Deterministic):**\n- ✓ ACCEPT: Bot processing → spinner visible within 300ms of question submission\n- ✓ ACCEPT: Tool call executing → contextual status message visible\n- ✓ ACCEPT: Response received → spinner and status messages removed, response displayed\n- ✓ ACCEPT: Timeout >10s → polite timeout error message shown with escalation prompt\n- ✓ TIMING: Spinner appears < 300ms after question submission\n**Clinical Safety:** N/A\n**Information Governance:** No data captured in loading states\n**Security:** Status messages must not expose internal system details or tool call internals\n**Observability:** Tool call duration per type, timeout rate, spinner-to-response duration\n**Frontend:** Spinner component, status message live region (aria-live="polite") — @emisgroup/ui-* components, i18n, jest-axe\n**Priority:** Must Have | **Effort:** Low | **Risk:** Low\n**Depends On:** REQ-001\nIs this correct?	\N	\N	\N	\N	\N	\N	2026-05-31 19:42:35.505094+00
0023bc4b-012c-4a19-bc78-8dcf12322d19	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	yes	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:42:37.624685+00
b500e473-95f4-422a-a040-c84bcea7ccd5	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	✅ REQ-006 confirmed — saving now.\n✅ REQ-006 saved.\nNow let me proceed to Phase 11 — finalising all artefacts. I'll first read the existing saved files to ensure the final versions are complete and consistent.\nGood — I have all artefacts. Now I need to note two important updates from the interview:\n1. **REQ-001** needs updating — authentication was confirmed as required (EMIS-X) for the widget, but the draft says "No authentication required"\n2. **manifest.md** needs a full final update\nLet me now run the **Phase 11 finalisation pass** — saving the updated manifest and corrected REQ-001 first.\n✅ manifest.md finalised (v2). Now updating REQ-001 to correct the authentication requirement:\n✅ REQ-001 updated (v2) — authentication correction applied.\nREQ-002 through REQ-006 are already final-quality from the progressive saves during the interview. The cross-cutting requirements gate is clear.\n---\n```\n═══════════════════════════════════════════════════════════════\n✅ PHASE 11 COMPLETE - REQUIREMENTS SPECIFICATION FINALISED\n═══════════════════════════════════════════════════════════════\n📦 OUTPUT FILES CREATED:\n───────────────────────────────────────────────────────────────\n📄 manifest.md (v2)\n   └─ Master blueprint with global standards and requirement index\n   └─ Project Code: QABOT-001\n📁 requirements/\n   ├─ REQ-001.md (v2 — auth correction applied)\n   ├─ REQ-002.md — Escalation to Human Support\n   ├─ REQ-003.md — Full-Page Chat Mode\n   ├─ REQ-004.md — Documentation Indexing via CI\n   ├─ REQ-005.md — Conversation Management\n   └─ REQ-006.md — Bot Loading & Status States\n═══════════════════════════════════════════════════════════════\n📊 STATISTICS:\n───────────────────────────────────────────────────────────────\nProduct Name: Q & A Interop Bot\nProject Code: QABOT-001\nTotal Requirements: 6\n└─ Must Have: 6\nGuardrails Referenced:\n├─ Information Governance: IG-001, IG-003\n├─ Security: AUTH-004, SEC-001, WSEC-006a, WSEC-013\n├─ Architecture: WA-005, AD-001, HTTP-002a, HTTP-003a\n├─ Design System: DS-001, DS-002, DS-004\n├─ Accessibility: A11Y-004a, A11Y-007a, A11Y-010\n└─ Coding Standards: WCS-007a, WCS-007b\n═══════════════════════════════════════════════════════════════\n```\n✅ Phase 11 complete → Proceeding to Phase 12: Feedback & Iteration Report\n✅ Pipeline 01 is complete. Feedback is optional — type **"skip"** at any time. The iteration report will be written automatically either way.\nHere's Q1 if you'd like to share:\n**How well did the requirements reflect what you described? (1–5, where 5 is perfect)**	\N	\N	\N	\N	\N	\N	2026-05-31 19:46:20.231119+00
2050bacd-fcc6-4f62-a661-5635e2ba23c6	c91b73d1-cbc4-42f7-979a-57288d51a9aa	user	skip	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:46:26.244013+00
7d66a7ed-4991-44f8-8c43-73f24369285a	c91b73d1-cbc4-42f7-979a-57288d51a9aa	assistant	Understood — writing the iteration report and updating the manifest now.\nGood — manifest is comprehensive and up to date. Now saving the final manifest with pipeline status updated and the iteration report.\nDone. Here's the handoff summary:\n---\n```\n✅ Pipeline 01 Requirements Complete\nNEXT STEP — Upload to Pipeline 03 Architecture:\nFiles to upload:\n1. manifest.md (v3)\n2. requirements/REQ-001.md through REQ-006.md\n3. feedback/ITERATION_REPORT_P01_i1.md (for agent learning)\nPipeline 03 will ADD Architecture sections to each requirement file.\nIt will also UPDATE the Evaluation Function Specification with\narchitecture-level checks.\n⚠️ BLOCKER before go-live: Confirm GDPR Article 6 lawful basis\n   for processing partner email addresses on escalation.\n   Assign IG-OWNER and RESOLUTION DATE before Pipeline 07.\n⚠️ BLOCKER for Pipeline 03: AI implementation approach (LLM / RAG /\n   embeddings / fine-tuned model) must be decided before build starts.\nPipeline status:\n✅ Pipeline 01 Complete — 6 requirements, ~35 eval checks\n⏳ Pipeline 03 Next — Architecture\n⏳ Pipeline 04 — Design (API contracts, OpenAPI)\n⏳ Pipeline 05 — PxD (UI/UX, EMIS-X component specs)\n⏳ Pipeline 06 — Clinical Safety (N/A — not safety-critical)\n⏳ Pipeline 07 — Normalisation\n⏳ Pipeline 08 — Planning\n```	\N	\N	\N	\N	\N	\N	2026-05-31 19:48:01.385584+00
c10b394e-befe-42f8-91fd-fd65d34b913d	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	user	Please begin.	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:48:50.8897+00
8cf24a4f-6389-4244-b6a4-2e8f14010ba0	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	assistant	I'll start by loading all the existing requirements artefacts to understand what we're working with.\nI've read all 6 requirements. Here's what I found:\n---\n**QABOT-001 — Q & A Interop Bot**\n**6 requirements, all Must Have**\n| # | Requirement | Key UI Flows |\n|---|---|---|\n| REQ-001 | Conversational Q&A Chat Widget | Widget, ask question, get answer with citations, deflection for out-of-scope |\n| REQ-002 | Escalation to Human Support | Email capture form, Teams webhook confirmation, in-widget success state |\n| REQ-003 | Full-Page Chat Mode | `/chat` route, widget ↔ full-page expand/minimise, shared session |\n| REQ-004 | Documentation Indexing via CI | CI/CD pipeline status — no direct UI, but bot "stale index" state needed |\n| REQ-005 | Conversation Management | "Clear conversation" button + confirmation dialog, session persistence across pages |\n| REQ-006 | Bot Loading & Status States | Spinner, tool call status messages ("Searching documentation…"), timeout error |\n**REQ-004 is backend-only** (GitHub Actions CI gate) — no user-facing UI to prototype.\n---\n**Phase 1: Flow Prioritisation**\nWhich flows should the prototype prioritise?\n- **A — Full coverage:** All 5 UI requirements (REQ-001, REQ-002, REQ-003, REQ-005, REQ-006) — every screen and state\n- **B — Core journey:** The end-to-end partner developer flow: open widget → ask question → see loading states → get answer with citation → escalate if needed\n- **C — Specific subset:** Tell me which 2–3 requirements matter most right now\nAlso — **what are the 2–3 flows or acceptance criteria you're most uncertain about?** For example:\n- How the widget ↔ full-page transition feels in practice\n- Whether the escalation form fits naturally in the widget\n- How the loading/tool-call status messages should be sequenced\n- Whether the "clear conversation" confirmation dialog feels right	\N	\N	\N	\N	\N	\N	2026-05-31 19:49:10.485717+00
404e7cdf-3c95-4dd9-82db-613496f9cb39	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	user	A	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:49:29.559352+00
80f6de9c-a5a5-4286-aa0a-c4d44a3c7fe6	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	assistant	Great — full coverage it is. Now let me ask about visual direction.\n**Phase 2: Visual Direction**\nAny visual preferences for the prototype?\n- **A — Clean and minimal** — system fonts, simple cards, blue primary (`#2563eb`)\n- **B — Healthcare professional** — clinical-feeling UI with clear hierarchy (not applicable here, but available)\n- **C — Match an existing product** — the bot lives inside an **Astro/Starlight documentation portal**, so I can style it to feel like a docs-site chat widget (dark sidebar, light content area, monospace accents)\n- **D — No preference** — I'll use a clean default that fits a developer documentation context\nAlso: do you have any wireframes or sketches to guide layouts? If not, I'll derive everything from the requirements.	\N	\N	\N	\N	\N	\N	2026-05-31 19:49:37.301735+00
1ca3f672-765c-4d81-bf94-01c886d47112	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	user	C - just the astro/starlight default	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:49:58.112451+00
aaa7ffba-6b59-41c6-9bca-ea94dda4c2fc	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	assistant	Perfect. I'll style it to match the Astro/Starlight default theme — dark sidebar, clean white content area, purple/indigo accent (`#7c3aed` / Starlight's signature violet), monospace code hints, and the characteristic docs-site layout feel.\nLet me now load all the requirement files to build the full screen plan.\nAll requirements loaded. Here's the **build plan** — please approve and I'll generate the full prototype.\n---\n## Prototype Build Plan\n**Theme:** Astro/Starlight default — dark sidebar (`#1e1e2e`), white content area, violet accent (`#7c3aed`), monospace font hints\n---\n### Screens & States\n| # | Screen / State | Covers |\n|---|---|---|\n| 1 | **Portal page (docs view)** | Base Starlight layout — dark sidebar, breadcrumbs, content. Widget visible bottom-right. |\n| 2 | **Widget — idle / closed** | Floating button with unread indicator. |\n| 3 | **Widget — open, empty** | Welcome message, input field. REQ-001 |\n| 4 | **Widget — loading** | Spinner + sequential tool call status messages ("Searching documentation…", "Retrieving examples…"). REQ-006 |\n| 5 | **Widget — answer with citation** | Bot reply, source URL, quoted lines. REQ-001 |\n| 6 | **Widget — out-of-scope deflection** | Polite deflection message, no citation. REQ-001 |\n| 7 | **Widget — escalation prompt** | "I couldn't find an answer — would you like to escalate?" + email form. REQ-002 |\n| 8 | **Widget — escalation email validation** | Invalid email inline error. REQ-002 |\n| 9 | **Widget — escalation success** | Confirmation message, form hidden. REQ-002 |\n| 10 | **Widget — timeout error** | "Sorry, that's taking longer than expected…" with escalation link. REQ-006 |\n| 11 | **Widget — clear conversation dialog** | Confirmation modal before clearing. REQ-005 |\n| 12 | **Full-page `/chat` view** | Same conversation, expand/minimise control, shared history. REQ-003 |\n| 13 | **Unauthenticated state** | Widget not rendered; redirect notice shown. REQ-001/REQ-003 |\n---\n### Navigation Flow\n```\nPortal page (authenticated)\n  └── Widget button (bottom-right)\n        └── Widget open → [ask question] → Loading → Answer / Deflection / Timeout\n                                         → [escalate] → Email form → Validation → Success\n                                         → [clear] → Confirmation dialog → Empty state\n                                         → [expand ↗] → Full-page /chat view\n                                                           → [← back] → Widget\nPortal page (unauthenticated)\n  └── Login redirect notice shown; widget absent\n```\n---\n### Hardcoded Demo Scenarios\n- **Scenario A (in-scope):** "Which auth flow should I use for a backend API?" → full answer + citation\n- **Scenario B (out-of-scope):** "What's the weather like today?" → deflection\n- **Scenario C (escalation):** "How do I handle token refresh for SMART on FHIR?" → bot can't answer → escalation form\n- **Scenario D (timeout):** Triggered via a "Simulate timeout" button in the demo toolbar\n---\nShall I proceed? Reply **"go"** (or any approval) and I'll build the full file.	\N	\N	\N	\N	\N	\N	2026-05-31 19:50:22.98044+00
d9b000e5-35b3-422c-afac-7ceae1b8960d	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	user	go	\N	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	\N	\N	2026-05-31 19:50:27.059144+00
c6ab8c8b-c0a0-40b8-b8f4-8f3bb2063de8	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	assistant	Let me load all the requirement files before building.\nAll requirements loaded. Now building the full prototype.\nBuilding the prototype now — this will be comprehensive. One moment...\nThe prototype is saved as `prototype/index.html`. Here's what's been built:\n---\n## ✅ Prototype Ready\n**Open `prototype/index.html` in any browser — no server needed.**\n---\n### What's in it\n| Screen / State | Requirement |\n|---|---|\n| Astro/Starlight portal shell — dark sidebar, content area, header | Base |\n| Floating widget FAB (bottom-right) with unread badge | REQ-001 |\n| Widget open — welcome message + suggestion chips | REQ-001 |\n| Loading states — animated dots + cycling status messages ("Searching documentation…", "Retrieving examples…", "Checking citations…", "Composing answer…") | REQ-006 |\n| Answer with citation card (source URL + quoted lines) | REQ-001 |\n| Out-of-scope deflection (amber bubble, polite message) | REQ-001 |\n| Escalation prompt — email form with RFC 5322 validation | REQ-002 |\n| Escalation email validation error state | REQ-002 |\n| Escalation success confirmation | REQ-002 |\n| Timeout error with retry + escalate links | REQ-006 |\n| Clear conversation — confirmation dialog | REQ-005 |\n| Expand ↗ to full-page `/chat` view (shared session) | REQ-003 |\n| Minimise ← back to widget (shared session) | REQ-003 |\n| Unauthenticated state — widget hidden, login prompt | REQ-001 / REQ-003 |\n### Demo toolbar (bottom-left)\n- **✓ Authenticated / 🔒 Unauthenticated** — toggle auth state\n- **💬 In-scope answer** — fires Scenario A (auth flow question with citation)\n- **🚫 Deflection** — fires Scenario B (weather question)\n- **📧 Escalation** — fires Scenario C (SMART token refresh, bot can't answer)\n- **⏱ Timeout** — fires Scenario D (timeout error)\n- **⏳ Loading states** — shows all 4 loading steps in sequence (3s)\n- **↺ Reset all** — clears everything back to initial state\n---\n> **Phase 4:** Click through the flows and tell me:\n> - What's missing or wrong?\n> - What feels confusing?\n> - What needs more detail?\n>\n> I'll update iteratively — no need to start over.	\N	\N	\N	\N	\N	\N	2026-05-31 19:55:14.692871+00
\.


--
-- Data for Name: parking_lot_item; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.parking_lot_item (parking_lot_item_id, conversation_id, content, priority, status, source_phase, resolved_at, created_at) FROM stdin;
02e3c5f2-ec62-4b16-b7a9-df10fb54b06a	c91b73d1-cbc4-42f7-979a-57288d51a9aa	AI implementation approach TBD — LLM, embeddings, fine-tuned model, or RAG. Pipeline 03 Architecture must decide before build starts.	critical	open	1	\N	2026-05-31 18:44:44.220338+00
b92c6ba7-4bb5-4d30-8d5f-cecc975b1d63	c91b73d1-cbc4-42f7-979a-57288d51a9aa	Existing Astro/Starlight portal architecture — React wrapper already in place for EMIS-X integration. Pipeline 03 needs to confirm component boundary and how the chat bot integrates with the existing shell.	high	open	2	\N	2026-05-31 18:46:10.900941+00
80fc9fcd-cf11-4532-8815-e93fa96c746e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	Teams channel webhook URL for escalation notifications — needs to be configured before go-live.	high	open	3	\N	2026-05-31 18:49:31.002598+00
e10a8b05-ddcb-4513-be53-fba73205965e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	GDPR Article 6 lawful basis for processing partner email addresses captured during escalation — IG-OWNER: TBD — RESOLUTION DATE: TBD — GO-LIVE BLOCKER if unresolved.	high	open	4	\N	2026-05-31 19:26:10.155797+00
\.


--
-- Data for Name: pipeline_stage; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.pipeline_stage (pipeline_stage_id, project_id, stage_type, status, iteration, started_at, completed_at, completed_by, sort_order) FROM stdin;
e6655423-b83f-43ed-9b4f-6ad819676c30	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	requirements_discovery	complete	1	2026-05-31 18:40:48.753823+00	2026-05-31 19:48:49.335812+00	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	1
007576f9-688f-4919-b276-9fb95537768d	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	prototype	in_progress	1	2026-05-31 19:48:50.811534+00	\N	\N	2
f3d3a924-9329-4d15-ad13-02aa9dab44b8	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	architecture	blocked	1	\N	\N	\N	3
79057a7d-ab5d-408e-a069-815439b6b254	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	design	blocked	1	\N	\N	\N	4
df7f8bc8-78b9-4a4d-8c1c-ad18e5817b77	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	pxd	blocked	1	\N	\N	\N	5
c4f49df3-cbe0-464a-9112-01f0dd0e12ef	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	clinical_safety	blocked	1	\N	\N	\N	6
2abbb994-538f-49e6-ba10-bf8d765008fe	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	normalisation	blocked	1	\N	\N	\N	7
eb39a1db-ead3-412e-a7d7-c059abc23f03	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	planning	blocked	1	\N	\N	\N	8
\.


--
-- Data for Name: project; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.project (project_id, code, name, description, compliance_domain, status, created_by, created_at, updated_at, is_deleted, time_sheet_code) FROM stdin;
03735ad1-8759-414e-a93f-ce8cc7bfc1fc	QABOT-001	Q & A Interop Bot	A chatbot for the partner portal that answers interoperability questions using RAG over existing documentation. Reduces developer sprint overhead spent answering partner questions from 20% to near-zero.	generic	in_progress	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	2026-05-31 18:40:42.332982+00	2026-05-31 18:40:48.754143+00	f	PORTASK0001045
\.


--
-- Data for Name: project_decision; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.project_decision (project_decision_id, project_id, title, context, decision, consequences, author_ern, author_given_name, author_family_name, created_at, updated_at) FROM stdin;
c3f6b2e4-3d50-4f7c-be81-2a3b4c5d6e7f	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	Host all infrastructure in AWS eu-west-2	The bot needs an infrastructure platform. AWS SES is already in use for email, and GDPR data residency requires EU hosting for partner email addresses.	All components (compute, storage, indexing) will be deployed to AWS in the eu-west-2 region.	Keeps personal data within the EU for GDPR compliance and consolidates tooling on AWS. Limits multi-cloud flexibility, which is acceptable for this project.	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	2026-05-31 20:10:00+00	2026-05-31 20:10:00+00
d4a7c3f5-4e61-4a8d-cf92-3b4c5d6e7f80	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	Index documentation as a pre-deployment CI gate	Documentation changes must be reflected in the bot without serving stale answers, and a failed index must not reach production.	Re-indexing runs as a blocking step in the GitHub Actions pipeline before the portal deploys; a failed index blocks deployment entirely.	Guarantees the index always matches the deployed docs. Adds a hard dependency on indexing success for every deployment, which is the intended safety trade-off.	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	2026-05-31 20:15:00+00	2026-05-31 20:15:00+00
\.


--
-- Data for Name: project_note; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.project_note (project_note_id, project_id, content, author_ern, author_given_name, author_family_name, created_at, updated_at) FROM stdin;
a1d4f0c2-1b3e-4d5a-9c6f-0e1a2b3c4d5e	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	Chase the platform team about the Teams webhook URL — we need it provisioned before the escalation flow can be demoed.	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	2026-05-31 20:01:00+00	2026-05-31 20:01:00+00
b2e5a1d3-2c4f-4e6b-ad70-1f2b3c4d5e6f	03735ad1-8759-414e-a93f-ce8cc7bfc1fc	Cost target reminder: prefer Bedrock Titan embeddings over hosted LLM APIs to keep running costs minimal per the constraints captured in discovery.	ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e	Luke	Smith	2026-05-31 20:05:00+00	2026-05-31 20:05:00+00
\.


--
-- Data for Name: token_usage; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.token_usage (token_usage_id, conversation_id, input_tokens, output_tokens, cache_read_input_tokens, cache_write_input_tokens, created_at) FROM stdin;
1fd15c51-e2da-4342-8aa0-17a216bf0084	c91b73d1-cbc4-42f7-979a-57288d51a9aa	23277	396	0	0	2026-05-31 18:40:54.007204+00
427c8e47-e02b-47a6-8104-68c4e192aa49	c91b73d1-cbc4-42f7-979a-57288d51a9aa	23694	2	0	0	2026-05-31 18:40:55.496044+00
16ba813e-b8eb-4264-8167-7ce2d88b0172	c91b73d1-cbc4-42f7-979a-57288d51a9aa	23614	178	0	0	2026-05-31 18:41:02.356808+00
23626328-7cd8-4b90-9aef-134d1ae0f3fa	c91b73d1-cbc4-42f7-979a-57288d51a9aa	23874	16	0	0	2026-05-31 18:41:03.554242+00
442087c4-450f-49ba-850c-720781622ba4	c91b73d1-cbc4-42f7-979a-57288d51a9aa	23776	168	0	0	2026-05-31 18:41:21.196993+00
93c1e304-7d7a-4a1f-872b-5a698b9e87cc	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24026	31	0	0	2026-05-31 18:41:23.82464+00
fb299bf0-a14b-4837-9b62-4457b5637109	c91b73d1-cbc4-42f7-979a-57288d51a9aa	23865	98	0	0	2026-05-31 18:41:37.572067+00
24547119-324f-4308-beaf-b325d567cc66	c91b73d1-cbc4-42f7-979a-57288d51a9aa	23984	40	0	0	2026-05-31 18:41:39.687331+00
ae18ff7e-7dc7-4e8c-bd3d-62a6b654c91a	c91b73d1-cbc4-42f7-979a-57288d51a9aa	23946	130	0	0	2026-05-31 18:42:21.291296+00
f71210ea-38ad-439f-9b16-06066ef181f2	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24097	2	0	0	2026-05-31 18:42:22.605575+00
dbb52769-c51b-4dce-9fc7-817026d2e46d	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24026	127	0	0	2026-05-31 18:42:56.461603+00
0bb0e653-3ac0-4129-8b0d-11101d9a7e23	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24174	2	0	0	2026-05-31 18:42:57.453646+00
6a711892-6090-46af-b892-85ba211c984e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24096	117	0	0	2026-05-31 18:43:09.914999+00
374c3330-76f5-4bda-879c-d23c928a0b61	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24234	2	0	0	2026-05-31 18:43:10.896059+00
65103ab0-897d-4aee-8c2a-7cf600c4236f	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24153	108	0	0	2026-05-31 18:43:17.632938+00
3c7ac2e8-3171-4530-9e7a-10123001111f	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24282	2	0	0	2026-05-31 18:43:18.668279+00
04c217f4-5297-47cd-8180-ca5d0bfd5b07	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24212	193	0	0	2026-05-31 18:44:44.211275+00
2bee3475-6e6f-48f7-baeb-5bb0888d934f	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24486	247	0	0	2026-05-31 18:44:50.601629+00
96dd6667-a1c0-4ac7-84aa-111281a01632	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24590	731	0	0	2026-05-31 18:45:05.46919+00
f84c53f5-65c1-4299-be4f-22070544b362	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25445	101	0	0	2026-05-31 18:45:08.925689+00
adcc9ca6-d103-4631-bcb2-dfbb0f3c3aeb	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24793	110	0	0	2026-05-31 18:45:22.085707+00
891083b8-83ed-43e1-881f-931585155681	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24924	2	0	0	2026-05-31 18:45:23.179997+00
302ead7e-396b-4a48-be5c-2314c663064c	c91b73d1-cbc4-42f7-979a-57288d51a9aa	24885	258	0	0	2026-05-31 18:46:10.893787+00
2d12fdfd-86c7-44f8-a51b-d39943b8a5b0	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25224	2	0	0	2026-05-31 18:46:12.03011+00
ca90eb7c-3aa3-49c2-9565-bfbcfd7cb6d7	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25062	121	0	0	2026-05-31 18:46:19.663882+00
a3df987a-ce54-4fb3-933f-91a5ff701a33	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25720	135	0	0	2026-05-31 18:46:21.961109+00
600eac67-7804-473a-a004-2565345b7bc7	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25939	33	0	0	2026-05-31 18:46:23.641026+00
52c1156c-f6b2-4ce2-bbf2-d48e2c99f74c	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25185	199	0	0	2026-05-31 18:47:11.651175+00
2c78fb5a-72f9-46bb-9e3e-61a371504ee1	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25983	2	0	0	2026-05-31 18:47:12.678951+00
b43584b1-e2f4-47be-bc01-a722e4c85aef	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25302	151	0	0	2026-05-31 18:47:52.076093+00
800967d7-cb58-43b0-ae5d-a4c98ce8c491	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25474	2	0	0	2026-05-31 18:47:53.124545+00
fbf54133-f5ee-49a9-b6cf-b02f6fa1f6e7	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25390	210	0	0	2026-05-31 18:48:05.826473+00
d2a058cc-f041-4152-9250-abb11b2a7b23	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25684	2	0	0	2026-05-31 18:48:06.848375+00
09f8f4f2-7482-4d3d-9a41-e51012e03136	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25535	219	0	0	2026-05-31 18:49:30.998166+00
78700aeb-7a7d-4ffa-b662-351b850e52ad	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25835	2	0	0	2026-05-31 18:49:32.046668+00
8f75fa67-dacd-4f16-9faf-d934c0831a11	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25673	193	0	0	2026-05-31 18:49:47.058182+00
f2b02e3c-72bf-478c-b66b-7aef057afcef	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25952	48	0	0	2026-05-31 18:49:48.466138+00
c72de2a7-7a9f-4872-a044-d69d65d0f8cd	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25771	301	0	0	2026-05-31 18:50:01.34171+00
7ce63960-84da-4a0b-b936-fa7d15de2125	c91b73d1-cbc4-42f7-979a-57288d51a9aa	26193	2	0	0	2026-05-31 18:50:02.401212+00
50679323-edde-4a37-99c3-516e8212e023	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25904	62	0	0	2026-05-31 18:50:36.749473+00
ce792912-713e-4ab0-a42f-ba15b9ed4aad	c91b73d1-cbc4-42f7-979a-57288d51a9aa	25997	45	0	0	2026-05-31 18:51:15.398551+00
6b0e8c82-bc04-4340-b7b3-a5ec96f6e264	c91b73d1-cbc4-42f7-979a-57288d51a9aa	26053	39	0	0	2026-05-31 18:51:27.93751+00
9781ec6d-f074-4c53-a1f7-504cbe18faf0	c91b73d1-cbc4-42f7-979a-57288d51a9aa	26101	56	0	0	2026-05-31 18:51:41.214046+00
5a91b428-aa43-4b7d-8782-6375af32f653	c91b73d1-cbc4-42f7-979a-57288d51a9aa	26166	37	0	0	2026-05-31 18:51:52.81242+00
f0fabbbe-895a-4a1c-bcb9-ba115bd20212	c91b73d1-cbc4-42f7-979a-57288d51a9aa	26216	49	0	0	2026-05-31 18:52:08.929227+00
ee448cb8-3973-4c65-98e4-cc03b53f04ae	c91b73d1-cbc4-42f7-979a-57288d51a9aa	26272	466	0	0	2026-05-31 18:52:27.878322+00
2eacc7ca-20b5-4803-ab10-c323a22f85af	c91b73d1-cbc4-42f7-979a-57288d51a9aa	26742	74	0	0	2026-05-31 19:26:05.217514+00
4dbc9fa5-4fe5-4c71-bb93-79efd61b13ce	c91b73d1-cbc4-42f7-979a-57288d51a9aa	27353	109	0	0	2026-05-31 19:26:07.438007+00
53dd3006-d479-4ccc-9467-e862a15795ad	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31297	122	0	0	2026-05-31 19:26:10.148697+00
9ba86edb-3f8a-4060-9b07-6a5ac4e67d4d	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31436	4229	0	0	2026-05-31 19:27:21.294294+00
34952027-e6fe-49de-b0a5-641af2bc4c3a	c91b73d1-cbc4-42f7-979a-57288d51a9aa	35697	93	0	0	2026-05-31 19:27:23.379612+00
f0b17db2-3245-44a5-883f-18846806cf30	c91b73d1-cbc4-42f7-979a-57288d51a9aa	35811	56	0	0	2026-05-31 19:27:26.509257+00
3f01e936-1751-4ba0-b879-42db9e472fbb	c91b73d1-cbc4-42f7-979a-57288d51a9aa	26950	121	0	0	2026-05-31 19:27:56.561337+00
1c8fe5a7-5e65-44b0-856e-ff6ce5b8940e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	27092	2	0	0	2026-05-31 19:27:58.547552+00
afd39c4c-96b1-49c8-a8cd-c4340ad6186a	c91b73d1-cbc4-42f7-979a-57288d51a9aa	26997	84	0	0	2026-05-31 19:28:30.089341+00
78ea27ee-636c-4299-a618-40d5cb392fdb	c91b73d1-cbc4-42f7-979a-57288d51a9aa	27092	63	0	0	2026-05-31 19:28:54.292946+00
c27e88af-c797-42ce-96fd-28da9ab5bd7e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	27162	56	0	0	2026-05-31 19:29:04.425411+00
408c11b3-6e35-45cf-ac00-7074bb152617	c91b73d1-cbc4-42f7-979a-57288d51a9aa	27237	57	0	0	2026-05-31 19:29:35.178343+00
0c8497f9-27d2-4758-a814-f2990e4365e3	c91b73d1-cbc4-42f7-979a-57288d51a9aa	27300	56	0	0	2026-05-31 19:29:55.137502+00
b9a8fc9c-0089-49d6-900c-77d0a4eeb29b	c91b73d1-cbc4-42f7-979a-57288d51a9aa	27363	446	0	0	2026-05-31 19:30:12.32152+00
c734cd97-58f5-483f-af60-2dd059e52b91	c91b73d1-cbc4-42f7-979a-57288d51a9aa	27813	3274	0	0	2026-05-31 19:31:08.004749+00
59cc620c-f1f4-4e97-9f1c-685fd8db9603	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31119	93	0	0	2026-05-31 19:31:10.244908+00
febe53d5-bb5d-40ce-a908-4b6e9bcfc0d7	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31233	67	0	0	2026-05-31 19:31:13.392394+00
1ae9e0d6-a2c3-419f-b34a-c6af3a8b1a45	c91b73d1-cbc4-42f7-979a-57288d51a9aa	27930	62	0	0	2026-05-31 19:31:31.011241+00
66447444-2156-44e1-962a-58c5ab9ca088	c91b73d1-cbc4-42f7-979a-57288d51a9aa	27996	72	0	0	2026-05-31 19:31:41.009675+00
36e311fa-4a32-4ae7-9a1d-b4db520e35e5	c91b73d1-cbc4-42f7-979a-57288d51a9aa	28073	402	0	0	2026-05-31 19:31:56.31554+00
09b2d18b-018e-4d30-b44d-2eb61c4ab73b	c91b73d1-cbc4-42f7-979a-57288d51a9aa	28481	46	0	0	2026-05-31 19:32:05.954394+00
4b6aba4e-61ff-4fce-bea8-676f6fc1c67d	c91b73d1-cbc4-42f7-979a-57288d51a9aa	28536	73	0	0	2026-05-31 19:32:15.067104+00
e9534536-cdfd-4c9f-b68f-1726f0c78d63	c91b73d1-cbc4-42f7-979a-57288d51a9aa	28613	506	0	0	2026-05-31 19:32:23.814022+00
e5ec27a3-457b-4062-9156-483b5060f6a3	c91b73d1-cbc4-42f7-979a-57288d51a9aa	29123	2542	0	0	2026-05-31 19:33:08.159137+00
63aaa629-a4e8-48cd-afc8-499fb3211373	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31697	93	0	0	2026-05-31 19:33:10.299905+00
9be1599a-d34b-4e9d-baea-99f449dae704	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31811	87	0	0	2026-05-31 19:33:13.656019+00
88d03f10-1c6b-4721-b428-0e5471505a5e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	29258	63	0	0	2026-05-31 19:33:38.548144+00
9fc53dd8-a06b-4fe8-a4b7-7448389cf093	c91b73d1-cbc4-42f7-979a-57288d51a9aa	29342	66	0	0	2026-05-31 19:34:10.635829+00
b24030a4-5a35-413f-9925-1a21e25399f8	c91b73d1-cbc4-42f7-979a-57288d51a9aa	29425	68	0	0	2026-05-31 19:34:34.226066+00
568a71d2-a5e7-4c41-a56c-898943681d78	c91b73d1-cbc4-42f7-979a-57288d51a9aa	29497	35	0	0	2026-05-31 19:34:43.727601+00
bfe30895-afc5-455c-bede-9aaa4d121fe1	c91b73d1-cbc4-42f7-979a-57288d51a9aa	29538	387	0	0	2026-05-31 19:34:57.038758+00
afbb3b6d-0533-4c5d-876b-72acce6f712b	c91b73d1-cbc4-42f7-979a-57288d51a9aa	29929	1846	0	0	2026-05-31 19:35:29.164291+00
024565e1-4060-4dc2-9347-2b59669509b1	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31807	93	0	0	2026-05-31 19:35:31.404162+00
6bfb50f6-eba4-4dfa-b790-fba2a7218362	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31921	82	0	0	2026-05-31 19:35:35.019897+00
32499ede-4836-4a79-a167-0b07b5208d7e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	30058	55	0	0	2026-05-31 19:35:52.321983+00
e7b665df-d0f4-4330-9277-ea82d0d6a8f3	c91b73d1-cbc4-42f7-979a-57288d51a9aa	30117	409	0	0	2026-05-31 19:36:33.120736+00
9399412f-d2ab-452d-99d3-21fe5fdadcba	c91b73d1-cbc4-42f7-979a-57288d51a9aa	30530	2549	0	0	2026-05-31 19:37:15.995518+00
f5cb106c-4b3f-450d-ba98-14292232f852	c91b73d1-cbc4-42f7-979a-57288d51a9aa	33111	93	0	0	2026-05-31 19:37:18.116967+00
c0edf465-2c1f-4f9f-bae9-4e64e6573324	c91b73d1-cbc4-42f7-979a-57288d51a9aa	33225	118	0	0	2026-05-31 19:37:22.225371+00
86864a8e-a0f8-47f1-ac63-eed07a2bc0d8	c91b73d1-cbc4-42f7-979a-57288d51a9aa	30705	236	0	0	2026-05-31 19:37:51.678417+00
4fed4c02-de3d-44dd-8489-9df786e13557	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31025	37	0	0	2026-05-31 19:37:53.577404+00
2857a59f-2046-440b-8518-f271e72c1d6e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	30853	44	0	0	2026-05-31 19:38:15.488653+00
537e51c2-548c-4937-9802-ff37bcffa7cd	c91b73d1-cbc4-42f7-979a-57288d51a9aa	30907	40	0	0	2026-05-31 19:38:23.485084+00
993f7d53-dc31-4404-b0da-a6820bb311e0	c91b73d1-cbc4-42f7-979a-57288d51a9aa	30952	56	0	0	2026-05-31 19:38:34.763866+00
4ab67910-a7a7-490a-8b89-74bf5f6b306e	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31020	79	0	0	2026-05-31 19:38:55.84019+00
448ca650-548d-4de8-b174-b7b28dabdcca	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31103	128	0	0	2026-05-31 19:39:04.258364+00
b691d598-34ba-4367-bf29-c969588658cc	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31242	67	0	0	2026-05-31 19:39:23.816779+00
068356dd-e18e-4dca-ae08-29051c3f0d07	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31327	67	0	0	2026-05-31 19:39:41.179118+00
f01e069e-ca0b-4a8e-8164-75933fff6bfb	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31407	81	0	0	2026-05-31 19:40:06.196476+00
f10873c1-e16d-43d3-a169-9bae92028b04	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31493	36	0	0	2026-05-31 19:40:19.594674+00
e87f6348-ef3c-44c4-b2a3-35ae4c812d3f	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31543	84	0	0	2026-05-31 19:40:41.396586+00
1ca10e9e-1b55-4027-954c-0a9081e513e7	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31633	74	0	0	2026-05-31 19:41:36.319298+00
a3ea92bd-1254-4363-88d0-51e7f3734b9b	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31718	97	0	0	2026-05-31 19:41:51.295702+00
ca05cacf-f488-4ccb-89f6-7d7944d39ff9	c91b73d1-cbc4-42f7-979a-57288d51a9aa	31819	229	0	0	2026-05-31 19:42:00.452496+00
775ca8db-f953-4869-a795-643f7487511d	c91b73d1-cbc4-42f7-979a-57288d51a9aa	32067	507	0	0	2026-05-31 19:42:35.499342+00
8419709a-75cb-43db-86fc-034c2e4fb2ae	c91b73d1-cbc4-42f7-979a-57288d51a9aa	32578	2685	0	0	2026-05-31 19:43:19.089522+00
c5388be2-9ffe-46f0-88fc-e902b81e4669	c91b73d1-cbc4-42f7-979a-57288d51a9aa	35295	106	0	0	2026-05-31 19:43:22.7233+00
c2ffce86-ddaa-4b98-bf1e-677f655ffa22	c91b73d1-cbc4-42f7-979a-57288d51a9aa	35938	111	0	0	2026-05-31 19:43:25.063048+00
400d78e9-38b6-4add-8037-3bb8f0d16252	c91b73d1-cbc4-42f7-979a-57288d51a9aa	43472	111	0	0	2026-05-31 19:43:27.645238+00
48ae7be8-9257-4660-b875-e7e1a317307c	c91b73d1-cbc4-42f7-979a-57288d51a9aa	47876	64	0	0	2026-05-31 19:43:30.284303+00
6216b865-92b6-4d4b-aae2-68306850d805	c91b73d1-cbc4-42f7-979a-57288d51a9aa	50420	183	0	0	2026-05-31 19:43:35.252273+00
00cc5ba6-9adb-4879-9dea-078304523bd0	c91b73d1-cbc4-42f7-979a-57288d51a9aa	50630	3451	0	0	2026-05-31 19:44:40.504792+00
077e1ccd-9154-4ac2-bf8c-d2b1852a8da9	c91b73d1-cbc4-42f7-979a-57288d51a9aa	54108	4694	0	0	2026-05-31 19:46:02.626837+00
d00faa74-7d08-407f-8d5a-dec982121948	c91b73d1-cbc4-42f7-979a-57288d51a9aa	58834	93	0	0	2026-05-31 19:46:05.564632+00
44445b8f-3201-4184-aa52-ab222398c956	c91b73d1-cbc4-42f7-979a-57288d51a9aa	58948	631	0	0	2026-05-31 19:46:16.791335+00
578a3e77-2a28-4c4b-85c6-196bf41dc956	c91b73d1-cbc4-42f7-979a-57288d51a9aa	59607	75	0	0	2026-05-31 19:46:20.226895+00
2c4a894e-37cd-459b-b336-63802abb6c87	c91b73d1-cbc4-42f7-979a-57288d51a9aa	33428	52	0	0	2026-05-31 19:46:28.613368+00
bcf2f527-6d14-4aab-a759-e05f59df5521	c91b73d1-cbc4-42f7-979a-57288d51a9aa	33680	59	0	0	2026-05-31 19:46:30.761026+00
a93a6a54-59de-4cef-9c23-ef5358cbe5fd	c91b73d1-cbc4-42f7-979a-57288d51a9aa	37136	3476	0	0	2026-05-31 19:47:12.634459+00
4398a865-0b3e-4987-add8-0c736e201c5f	c91b73d1-cbc4-42f7-979a-57288d51a9aa	40639	1921	0	0	2026-05-31 19:47:50.913236+00
2f381b4b-b6d4-4e08-ae54-1c803334ea1a	c91b73d1-cbc4-42f7-979a-57288d51a9aa	42599	93	0	0	2026-05-31 19:47:53.46407+00
c3467444-2207-4716-b9df-56e7baafcad8	c91b73d1-cbc4-42f7-979a-57288d51a9aa	42713	355	0	0	2026-05-31 19:48:01.379973+00
f67053ee-6ac1-4f2e-8da0-efff2dc326fe	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	4938	59	0	0	2026-05-31 19:48:52.309211+00
8fd3bac8-a2cd-42f3-8747-e39688e29182	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	5230	200	0	0	2026-05-31 19:48:54.484547+00
c21392e3-88d2-4a95-971e-2a5f40106c68	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	19192	158	0	0	2026-05-31 19:48:56.749226+00
63d0d201-a97f-4547-8a37-6221832b5f17	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	26281	134	0	0	2026-05-31 19:48:59.050409+00
eb6eb068-c71a-42b9-b197-3eed9170828f	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	26498	489	0	0	2026-05-31 19:49:10.481544+00
1591e098-c037-439e-9e2b-7666d913384e	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	5462	90	0	0	2026-05-31 19:49:32.324386+00
d5071a24-371d-49fe-8b61-5e90bcae2c63	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	5573	169	0	0	2026-05-31 19:49:37.294082+00
14aa7ee9-f760-4369-8f8e-2598291c112c	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	5660	185	0	0	2026-05-31 19:50:01.900992+00
0fc3f56e-463f-4fe5-b308-70e36b13ddf8	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	13899	205	0	0	2026-05-31 19:50:04.297419+00
7a1aec7a-6dcf-417d-b3fa-d0f3c888834a	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	24951	152	0	0	2026-05-31 19:50:06.791808+00
73a850c2-6036-456a-ae7c-3008ea1c7981	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	25185	796	0	0	2026-05-31 19:50:22.976576+00
39d72c17-7aa3-4c58-9afc-b50055bf290f	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	6538	117	0	0	2026-05-31 19:50:29.019955+00
c4ba0c91-e99d-4309-bb4d-b5df9cde5684	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	14709	205	0	0	2026-05-31 19:50:31.506769+00
795653d1-161c-4949-acf9-172ebcc35ba7	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	25761	104	0	0	2026-05-31 19:50:33.879559+00
edc26dfd-aa34-4190-8960-77cfdca31f75	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	25886	26340	0	0	2026-05-31 19:54:57.492655+00
73284a0c-5c4b-448d-a469-a6dec94e9679	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	52255	93	0	0	2026-05-31 19:55:00.320528+00
3d1b71b8-02cb-4bc2-b191-a6f565b0ac24	71a9af59-f14c-4ed0-842c-7c6aa74c8b3f	52369	604	0	0	2026-05-31 19:55:14.688729+00
\.


--
-- Name: flyway_schema_history flyway_schema_history_pk; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.flyway_schema_history
    ADD CONSTRAINT flyway_schema_history_pk PRIMARY KEY (installed_rank);


--
-- Name: artefact pk_artefact; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.artefact
    ADD CONSTRAINT pk_artefact PRIMARY KEY (artefact_id);


--
-- Name: conversation pk_conversation; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conversation
    ADD CONSTRAINT pk_conversation PRIMARY KEY (conversation_id);


--
-- Name: message pk_message; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.message
    ADD CONSTRAINT pk_message PRIMARY KEY (message_id);


--
-- Name: parking_lot_item pk_parking_lot_item; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.parking_lot_item
    ADD CONSTRAINT pk_parking_lot_item PRIMARY KEY (parking_lot_item_id);


--
-- Name: pipeline_stage pk_pipeline_stage; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pipeline_stage
    ADD CONSTRAINT pk_pipeline_stage PRIMARY KEY (pipeline_stage_id);


--
-- Name: project pk_project; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.project
    ADD CONSTRAINT pk_project PRIMARY KEY (project_id);


--
-- Name: project_decision pk_project_decision; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.project_decision
    ADD CONSTRAINT pk_project_decision PRIMARY KEY (project_decision_id);


--
-- Name: project_note pk_project_note; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.project_note
    ADD CONSTRAINT pk_project_note PRIMARY KEY (project_note_id);


--
-- Name: token_usage pk_token_usage; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.token_usage
    ADD CONSTRAINT pk_token_usage PRIMARY KEY (token_usage_id);


--
-- Name: flyway_schema_history_s_idx; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX flyway_schema_history_s_idx ON public.flyway_schema_history USING btree (success);


--
-- Name: idx_artefact_project_filepath; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_artefact_project_filepath ON public.artefact USING btree (project_id, file_path);


--
-- Name: idx_artefact_project_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_artefact_project_id ON public.artefact USING btree (project_id);


--
-- Name: idx_conversation_stage_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_conversation_stage_id ON public.conversation USING btree (stage_id);


--
-- Name: idx_message_conversation_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_message_conversation_id ON public.message USING btree (conversation_id);


--
-- Name: idx_message_created_at; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_message_created_at ON public.message USING btree (conversation_id, created_at);


--
-- Name: idx_parking_lot_item_conversation_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_parking_lot_item_conversation_id ON public.parking_lot_item USING btree (conversation_id);


--
-- Name: idx_parking_lot_item_status; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_parking_lot_item_status ON public.parking_lot_item USING btree (conversation_id, status);


--
-- Name: idx_pipeline_stage_project_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_pipeline_stage_project_id ON public.pipeline_stage USING btree (project_id);


--
-- Name: idx_pipeline_stage_project_id_sort_order; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_pipeline_stage_project_id_sort_order ON public.pipeline_stage USING btree (project_id, sort_order);


--
-- Name: idx_project_decision_project_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_project_decision_project_id ON public.project_decision USING btree (project_id);


--
-- Name: idx_project_note_project_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_project_note_project_id ON public.project_note USING btree (project_id);


--
-- Name: idx_project_status; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_project_status ON public.project USING btree (status);


--
-- Name: idx_token_usage_conversation_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_token_usage_conversation_id ON public.token_usage USING btree (conversation_id);


--
-- Name: idx_uq_project_code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX idx_uq_project_code ON public.project USING btree (code) WHERE (is_deleted = false);


--
-- Name: artefact fk_artefact_project_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.artefact
    ADD CONSTRAINT fk_artefact_project_id FOREIGN KEY (project_id) REFERENCES public.project(project_id) ON DELETE CASCADE;


--
-- Name: conversation fk_conversation_stage_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conversation
    ADD CONSTRAINT fk_conversation_stage_id FOREIGN KEY (stage_id) REFERENCES public.pipeline_stage(pipeline_stage_id) ON DELETE CASCADE;


--
-- Name: message fk_message_conversation_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.message
    ADD CONSTRAINT fk_message_conversation_id FOREIGN KEY (conversation_id) REFERENCES public.conversation(conversation_id) ON DELETE CASCADE;


--
-- Name: parking_lot_item fk_parking_lot_item_conversation_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.parking_lot_item
    ADD CONSTRAINT fk_parking_lot_item_conversation_id FOREIGN KEY (conversation_id) REFERENCES public.conversation(conversation_id) ON DELETE CASCADE;


--
-- Name: pipeline_stage fk_pipeline_stage_project_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pipeline_stage
    ADD CONSTRAINT fk_pipeline_stage_project_id FOREIGN KEY (project_id) REFERENCES public.project(project_id) ON DELETE CASCADE;


--
-- Name: project_decision fk_project_decision_project_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.project_decision
    ADD CONSTRAINT fk_project_decision_project_id FOREIGN KEY (project_id) REFERENCES public.project(project_id) ON DELETE CASCADE;


--
-- Name: project_note fk_project_note_project_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.project_note
    ADD CONSTRAINT fk_project_note_project_id FOREIGN KEY (project_id) REFERENCES public.project(project_id) ON DELETE CASCADE;


--
-- Name: token_usage fk_token_usage_conversation_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.token_usage
    ADD CONSTRAINT fk_token_usage_conversation_id FOREIGN KEY (conversation_id) REFERENCES public.conversation(conversation_id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

