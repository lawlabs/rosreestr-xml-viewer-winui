<?xml version = "1.0" encoding = "UTF-8"?>
<xsl:stylesheet version = "1.0" xmlns:xsl = "http://www.w3.org/1999/XSL/Transform">   

	<xsl:template match = "/"> 	
		<html> 
  
			<xsl:call-template name="style_tables"/>
						
			<body> 

            <center><h2>Выписка об объекте недвижимости (помещении)</h2></center> 
			<br/>				

			<xsl:apply-templates select="extract_about_property_room/details_statement/group_top_requisites"/>
            
			<xsl:apply-templates select="extract_about_property_room/room_record"/>
			
			<xsl:apply-templates select="extract_about_property_room/right_records"/>			
								        				
			</body> 
		</html> 		
	 
	</xsl:template>  	 
	
	<xsl:include href="https://legalcalc.ru/xslt/group_top_requisites.xsl"/>
	          
    <xsl:template match="room_record">				
		<br/><br/><br/>
		<center><h3>Сведения об объекте недвижимости</h3></center>
				
		<table>
			<tr><td>Дата постановки на учет/ регистрации: </td> <td><xsl:call-template name="formatDateTime"><xsl:with-param name="dateTime" select="record_info/registration_date"/></xsl:call-template></td></tr>			
			<tr><td>Дата снятия с учета/регистрации: </td> <td><xsl:call-template name="formatDateTime"><xsl:with-param name="dateTime" select="record_info/cancel_date"/></xsl:call-template></td></tr>						
			<tr><td colspan="2"  class="row_group_1"><center>Общие сведения.</center></td></tr>
			<xsl:apply-templates select="object/common_data"/>
			<xsl:apply-templates select="cad_links"/>
			<xsl:apply-templates select="params"/>
			<xsl:apply-templates select="address_room/address/address"/>
			<xsl:apply-templates select="location_in_build"/>
			<xsl:apply-templates select="cost"/>						
		</table>						       
		
    </xsl:template> 

	<xsl:include href="https://legalcalc.ru/xslt/styles/style_tables.xsl"/>	   
	
	<xsl:include href="https://legalcalc.ru/xslt/room_record/record_info.xsl"/>
	
	<xsl:include href="https://legalcalc.ru/xslt/right_records.xsl"/>   
   
</xsl:stylesheet>